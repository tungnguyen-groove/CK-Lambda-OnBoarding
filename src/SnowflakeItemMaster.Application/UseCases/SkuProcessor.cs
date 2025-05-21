using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Contracts.MessageQueue;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using SnowflakeItemMaster.Application.Contracts.Provider;
using SnowflakeItemMaster.Application.Dtos;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Application.Settings;
using SnowflakeItemMaster.Domain.Entities;
using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.UseCases
{
    public class SkuProcessor : ISkuProcessor
    {
        private readonly IItemPetalRepository _itemPetalRepository;
        private readonly ITransformService _transformService;
        private readonly IRepositoryManager _repositoryManager;
        private readonly IMessageQueueService _messageQueue;
        private readonly ILoggingService _logger;
        private PerformanceConfigs _performanceConfigs;
        private SchedulerConfigs _schedulerConfigs;
        private readonly JsonSerializerOptions _jsonOptions;

        public SkuProcessor(
            IItemPetalRepository itemPetalRepository,
            ITransformService transformService,
            IMessageQueueService messageQueue,
            ILoggingService logger,
            IRepositoryManager repositoryManager,
            IConfigWrapper configWrapper)
        {
            _itemPetalRepository = itemPetalRepository;
            _transformService = transformService;
            _messageQueue = messageQueue;
            _logger = logger;
            _repositoryManager = repositoryManager;
            _performanceConfigs = configWrapper.GetPerformanceConfigs() ?? throw new ArgumentNullException(nameof(_performanceConfigs));
            _schedulerConfigs = configWrapper.GetSchedulerConfigs() ?? throw new ArgumentNullException(nameof(_schedulerConfigs));
            _jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        public async Task<SkuProcessingResponseDto> ProcessSkusAsync(SkuRequestDto request)
        {
            var result = new SkuProcessingResponseDto();

            // 1. Query items from Snowflake
            var itemPetals = await GetItemPetalsFromSnowflakeAsync(request.Skus);
            if (!itemPetals.Any())
            {
                _logger.LogWarning("No data found in Snowflake for the requested SKUs");
                return SkuProcessingResponseDto.CreateError("No data found in Snowflake");
            }

            result.TotalSKUs = request.Skus.Count;
            _logger.LogInfo($"Retrieved {itemPetals.Count} items from Snowflake");

            // 2. Transform and validate (e.i: with batching if > 100 items)
            var processedItems = await TransformAndValidateItemsAsync(itemPetals);

            // 3. Process items and track results (e.i: with batching if > 100 items)
            await ProcessItemsAsync(processedItems, result);

            return result;
        }

        private async Task<List<ItemPetal>> GetItemPetalsFromSnowflakeAsync(IList<string> skus)
        {
            var result = skus.Any()
                    ? await _itemPetalRepository.GetItemsBySkusAsync(skus)
                    : await _itemPetalRepository.GetItemsWithLimitAsync(_schedulerConfigs.Limit, _schedulerConfigs.Hours);

            return result;
        }

        private async Task<List<ProcessedItemResult>> TransformAndValidateItemsAsync(List<ItemPetal> itemPetals)
        {
            if (itemPetals == null || !itemPetals.Any())
                return new List<ProcessedItemResult>();

            // If items <= 100, use original parallel processing
            if (itemPetals.Count <= _performanceConfigs.BatchSize)
            {
                return await TransformAndValidateItemsParallelAsync(itemPetals);
            }

            // If items > 100, use batch processing
            return await TransformAndValidateItemsInBatchesAsync(itemPetals);
        }

        private async Task<List<ProcessedItemResult>> TransformAndValidateItemsParallelAsync(List<ItemPetal> itemPetals)
        {
            if (itemPetals == null || !itemPetals.Any())
                return new List<ProcessedItemResult>();

            var processedItems = new ConcurrentBag<ProcessedItemResult>();

            await Parallel.ForEachAsync(
                itemPetals,
                new ParallelOptions { MaxDegreeOfParallelism = _performanceConfigs.ParallelDegree },
                (itemPetal, cancellationToken) =>
                {
                    try
                    {
                        // Transform to ItemMasterSourceLog and validate
                        var sourceLog = _transformService.TransformItemPetalToItemMasterSourceLog(itemPetal);

                        // Transform valid items to UnifiedItemModel
                        UnifiedItemModel unifiedModel = new();
                        if (sourceLog.ValidationStatus == Status.Valid)
                        {
                            unifiedModel = _transformService.MapToUnifiedItemMaster(itemPetal);
                            sourceLog.CommonModel = JsonSerializer.Serialize(unifiedModel, _jsonOptions);
                        }

                        processedItems.Add(new ProcessedItemResult
                        {
                            SourceLog = sourceLog,
                            UnifiedModel = unifiedModel,
                            IsValid = sourceLog.ValidationStatus == Status.Valid
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing item with SKU {itemPetal?.Sku}: {ex.Message}");

                        processedItems.Add(new ProcessedItemResult
                        {
                            SourceLog = new ItemMasterSourceLog
                            {
                                Sku = itemPetal?.Sku ?? "Unknown",
                                ValidationStatus = Status.Invalid,
                                Errors = $"Processing error: {ex.Message}",
                                CreatedAt = DateTime.UtcNow
                            },
                            UnifiedModel = null,
                            IsValid = false
                        });
                    }

                    return new ValueTask();
                });

            return processedItems.ToList();
        }

        private async Task<List<ProcessedItemResult>> TransformAndValidateItemsInBatchesAsync(List<ItemPetal> itemPetals)
        {
            var allProcessedItems = new List<ProcessedItemResult>();
            var batchSize = _performanceConfigs.BatchSize;
            var totalBatches = (int)Math.Ceiling((double)itemPetals.Count / batchSize);

            _logger.LogInfo($"Processing {itemPetals.Count} items in {totalBatches} batches of size {batchSize}");

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = itemPetals.Skip(i * batchSize).Take(batchSize).ToList();
                _logger.LogInfo($"Processing batch {i + 1}/{totalBatches} with {batch.Count} items");

                var batchResult = await TransformAndValidateItemsParallelAsync(batch);
                allProcessedItems.AddRange(batchResult);

                // Add delay between batches to prevent resource exhaustion
                if (i < totalBatches - 1)
                {
                    await Task.Delay(100); // 100ms delay between batches
                }
            }

            return allProcessedItems;
        }

        private async Task ProcessItemsAsync(List<ProcessedItemResult> items, SkuProcessingResponseDto result)
        {
            // If items <= 100, use original parallel processing
            if (items.Count <= _performanceConfigs.BatchSize)
            {
                await ProcessItemsInParallelAsync(items, result);
                return;
            }

            // If items > 100, use batch processing
            await ProcessItemsInBatchesAsync(items, result);
        }

        private async Task ProcessItemsInParallelAsync(List<ProcessedItemResult> items, SkuProcessingResponseDto result)
        {
            try
            {
                // 1. Save source logs to database
                var sourceLogs = items.Where(x => x.SourceLog != null)
                    .Select(x => x.SourceLog)
                    .ToList();

                _repositoryManager.ItemMasterSourceLog.AddRange(sourceLogs!);
                await _repositoryManager.SaveAsync();
                result.LogSaved = sourceLogs.Count;

                // 2. Process valid items
                var validItems = items.Where(i => i.IsValid && i.UnifiedModel != null).ToList();
                result.ValidSKUs = validItems.Count;
                result.InvalidSKUs = items.Count - validItems.Count;

                // 3. Process SQS messages in parallel
                var sqsResults = new ConcurrentBag<(int Id, bool Success, string Error)>();
                int successCount = 0;
                int failedCount = 0;

                await Parallel.ForEachAsync(
                    validItems,
                    new ParallelOptions { MaxDegreeOfParallelism = _performanceConfigs.ParallelDegree },
                    async (item, ct) =>
                    {
                        try
                        {
                            (bool success, string messsage) = await _messageQueue.PublishItemAsync(item.UnifiedModel!);
                            string errorMessage = success ? string.Empty : messsage;
                            sqsResults.Add((item.SourceLog!.Id, success, errorMessage));

                            if (success)
                            {
                                Interlocked.Increment(ref successCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref failedCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error publishing SKU {item.SourceLog!.Sku} to SQS: {ex.Message}");
                            sqsResults.Add((item.SourceLog.Id, false, ex.Message));
                            Interlocked.Increment(ref failedCount);
                        }
                    });

                result.SqsSuccess = successCount;
                result.SqsFailed = failedCount;

                // 4. Batch update database with SQS results
                await UpdateItemSourceLogWithSQSResultAsync(sqsResults.ToList());

                // 5. Update response details
                CreateResponseDetail(items, result, sqsResults.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessItemsInParallelAsync: {ex}");
                throw;
            }
        }

        private async Task ProcessItemsInBatchesAsync(List<ProcessedItemResult> items, SkuProcessingResponseDto result)
        {
            try
            {
                var batchSize = _performanceConfigs.BatchSize;
                var totalBatches = (int)Math.Ceiling((double)items.Count / batchSize);
                var allSqsResults = new List<(int Id, bool Success, string Error)>();

                _logger.LogInfo($"Processing {items.Count} items in {totalBatches} batches for database and SQS operations");

                // Initialize counters
                int totalLogSaved = 0;
                int totalValidSKUs = 0;
                int totalInvalidSKUs = 0;
                int totalSqsSuccess = 0;
                int totalSqsFailed = 0;

                for (int i = 0; i < totalBatches; i++)
                {
                    var batch = items.Skip(i * batchSize).Take(batchSize).ToList();
                    _logger.LogInfo($"Processing batch {i + 1}/{totalBatches} with {batch.Count} items");

                    // 1. Save source logs for this batch
                    var sourceLogs = batch.Where(x => x.SourceLog != null)
                        .Select(x => x.SourceLog)
                        .ToList();

                    _repositoryManager.ItemMasterSourceLog.AddRange(sourceLogs!);
                    await _repositoryManager.SaveAsync();
                    totalLogSaved += sourceLogs.Count;

                    // 2. Process valid items in this batch
                    var validItems = batch.Where(item => item.IsValid && item.UnifiedModel != null).ToList();
                    totalValidSKUs += validItems.Count;
                    totalInvalidSKUs += batch.Count - validItems.Count;

                    // 3. Process SQS messages for this batch in parallel
                    var batchSqsResults = new ConcurrentBag<(int Id, bool Success, string Error)>();
                    int batchSuccessCount = 0;
                    int batchFailedCount = 0;

                    await Parallel.ForEachAsync(
                        validItems,
                        new ParallelOptions { MaxDegreeOfParallelism = _performanceConfigs.ParallelDegree },
                        async (item, ct) =>
                        {
                            try
                            {
                                (bool success, string messsage) = await _messageQueue.PublishItemAsync(item.UnifiedModel!);
                                string errorMessage = success ? string.Empty : messsage;
                                batchSqsResults.Add((item.SourceLog!.Id, success, errorMessage));

                                if (success)
                                {
                                    Interlocked.Increment(ref batchSuccessCount);
                                }
                                else
                                {
                                    Interlocked.Increment(ref batchFailedCount);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Error publishing SKU {item.SourceLog!.Sku} to SQS: {ex.Message}");
                                batchSqsResults.Add((item.SourceLog.Id, false, ex.Message));
                                Interlocked.Increment(ref batchFailedCount);
                            }
                        });

                    totalSqsSuccess += batchSuccessCount;
                    totalSqsFailed += batchFailedCount;

                    // 4. Update database with SQS results for this batch
                    var batchSqsResultsList = batchSqsResults.ToList();
                    await UpdateItemSourceLogWithSQSResultAsync(batchSqsResultsList);
                    allSqsResults.AddRange(batchSqsResultsList);

                    // Optional: Add delay between batches
                    if (i < totalBatches - 1)
                    {
                        await Task.Delay(100); // 100ms delay between batches
                    }
                }

                // Set final results
                result.LogSaved = totalLogSaved;
                result.ValidSKUs = totalValidSKUs;
                result.InvalidSKUs = totalInvalidSKUs;
                result.SqsSuccess = totalSqsSuccess;
                result.SqsFailed = totalSqsFailed;

                // 5. Create response details
                CreateResponseDetail(items, result, allSqsResults);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ProcessItemsInBatchesAsync: {ex}");
                throw;
            }
        }

        private static void CreateResponseDetail(
            List<ProcessedItemResult> items,
            SkuProcessingResponseDto result,
            List<(int Id, bool Success, string Error)> sqsResults)
        {
            foreach (var item in items)
            {
                var sqsResult = sqsResults.FirstOrDefault(x => x.Id == item.SourceLog?.Id);
                result.Details.Add(new SkuDetailResponseDto
                {
                    Sku = item.SourceLog?.Sku ?? string.Empty,
                    IsValid = item.IsValid,
                    SentToSQS = sqsResult.Success,
                    Error = item.IsValid ? sqsResult.Error : item.SourceLog?.Errors
                });
            }
        }

        private async Task UpdateItemSourceLogWithSQSResultAsync(List<(int Id, bool Success, string Error)> sqsResults)
        {
            var successIds = sqsResults
                .Where(x => x.Success)
                .Select(x => x.Id)
                .ToList();

            var failedResults = sqsResults
                .Where(x => !x.Success)
                .ToList();

            if (successIds.Any())
            {
                await _repositoryManager.ItemMasterSourceLog.BatchUpdateSentStatusAsync(
                    successIds,
                    true,
                    null);
            }

            if (failedResults.Any())
            {
                var itemsToUpdate = _repositoryManager.ItemMasterSourceLog
                    .FindByCondition(x => failedResults.Select(f => f.Id).Contains(x.Id), true)
                    .ToList();

                foreach (var item in itemsToUpdate)
                {
                    var failedResult = failedResults.First(x => x.Id == item.Id);
                    item.IsSentToSqs = false;
                    item.Errors = failedResult.Error;
                    item.CreatedAt = DateTime.UtcNow;
                    _repositoryManager.ItemMasterSourceLog.Update(item);
                }
            }

            await _repositoryManager.SaveAsync();
        }

        [Obsolete("This method is not used in the current implementation. Use TransformAndValidateItemsParallelAsync instead.")]
        private List<ProcessedItemResult> TransformAndValidateItems(List<ItemPetal> itemPetals)
        {
            var processedItems = new List<ProcessedItemResult>();

            foreach (var itemPetal in itemPetals)
            {
                // Transform to ItemMasterSourceLog and validate
                var sourceLog = _transformService.TransformItemPetalToItemMasterSourceLog(itemPetal);

                // Transform valid items to UnifiedItemModel
                UnifiedItemModel unifiedModel = new();
                if (sourceLog.ValidationStatus == Status.Valid)
                {
                    unifiedModel = _transformService.MapToUnifiedItemMaster(itemPetal);
                    sourceLog.CommonModel = JsonSerializer.Serialize(unifiedModel);
                }

                processedItems.Add(new ProcessedItemResult
                {
                    SourceLog = sourceLog,
                    UnifiedModel = unifiedModel,
                    IsValid = sourceLog.ValidationStatus == Status.Valid
                });
            }

            return processedItems;
        }

        [Obsolete("This method is not used in the current implementation. Use ProcessItemsInParallelAsync instead.")]
        private async Task ProcessItemsWithTracking(List<ProcessedItemResult> items, SkuProcessingResponseDto result)
        {
            foreach (var item in items)
            {
                var skuDetail = new SkuDetailResponseDto
                {
                    Sku = item.SourceLog?.Sku ?? string.Empty,
                    IsValid = item.IsValid
                };

                try
                {
                    // Save to MySQL
                    _repositoryManager.ItemMasterSourceLog.Create(item.SourceLog!);
                    await _repositoryManager.SaveAsync();
                    result.LogSaved++;

                    if (item.IsValid)
                    {
                        result.ValidSKUs++;

                        // Publish to SQS if valid
                        if (item.UnifiedModel != null)
                        {
                            (bool success, string message) = await _messageQueue.PublishItemAsync(item.UnifiedModel);
                            if (success)
                            {
                                await _repositoryManager.ItemMasterSourceLog.UpdateSentStatusAsync(
                                    item.SourceLog!.Id,
                                    true);
                                result.SqsSuccess++;
                                skuDetail.SentToSQS = true;
                                await _repositoryManager.SaveAsync();
                            }
                            else
                            {
                                await _repositoryManager.ItemMasterSourceLog.UpdateSentStatusAsync(
                                    item.SourceLog!.Id,
                                    false,
                                    "Failed to publish to SQS");
                                result.SqsFailed++;
                                skuDetail.Error = "Failed to publish to SQS";
                                await _repositoryManager.SaveAsync();
                            }
                        }
                    }
                    else
                    {
                        result.InvalidSKUs++;
                        skuDetail.Error = item.SourceLog?.Errors;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error processing SKU {skuDetail.Sku}: {ex}");
                    skuDetail.Error = ex.Message;
                    result.SqsFailed++;

                    if (item.SourceLog?.Id != 0)
                    {
                        await _repositoryManager.ItemMasterSourceLog.UpdateSentStatusAsync(
                            item.SourceLog!.Id,
                            false,
                            ex.Message);
                        await _repositoryManager.SaveAsync();
                    }
                }

                result.Details.Add(skuDetail);
            }
        }
    }
}