using FluentAssertions;
using Moq;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Contracts.MessageQueue;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using SnowflakeItemMaster.Application.Contracts.Provider;
using SnowflakeItemMaster.Application.Dtos;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Application.Settings;
using SnowflakeItemMaster.Application.UseCases;
using SnowflakeItemMaster.Domain.Entities;
using SnowflakeItemMaster.Domain.Providers;

namespace SnowflakeItemMaster.Application.Tests
{
    public class SkuProcessorTests
    {
        private readonly Mock<IItemPetalRepository> _mockItemPetalRepository;
        private readonly Mock<ITransformService> _mockTransformService;
        private readonly Mock<IMessageQueueService> _mockMessageQueue;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<IRepositoryManager> _mockRepositoryManager;
        private readonly Mock<IConfigWrapper> _mockConfigWrapper;
        private readonly Mock<IItemMasterSourceLogRepository> _mockItemMasterSourceLogRepository;
        private readonly PerformanceConfigs _performanceConfigs;
        private readonly SchedulerConfigs _schedulerConfigs;
        private readonly SkuProcessor _skuProcessor;

        public SkuProcessorTests()
        {
            _mockItemPetalRepository = new Mock<IItemPetalRepository>();
            _mockTransformService = new Mock<ITransformService>();
            _mockMessageQueue = new Mock<IMessageQueueService>();
            _mockLogger = new Mock<ILoggingService>();
            _mockRepositoryManager = new Mock<IRepositoryManager>();
            _mockConfigWrapper = new Mock<IConfigWrapper>();
            _mockItemMasterSourceLogRepository = new Mock<IItemMasterSourceLogRepository>();

            _performanceConfigs = new PerformanceConfigs { BatchSize = 2, ParallelDegree = 2 };
            _schedulerConfigs = new SchedulerConfigs { Limit = 10, Hours = 1 };

            _mockConfigWrapper.Setup(c => c.GetPerformanceConfigs()).Returns(_performanceConfigs);
            _mockConfigWrapper.Setup(c => c.GetSchedulerConfigs()).Returns(_schedulerConfigs);

            _mockRepositoryManager.Setup(r => r.ItemMasterSourceLog)
                .Returns(_mockItemMasterSourceLogRepository.Object);

            _skuProcessor = new SkuProcessor(
                _mockItemPetalRepository.Object,
                _mockTransformService.Object,
                _mockMessageQueue.Object,
                _mockLogger.Object,
                _mockRepositoryManager.Object,
                _mockConfigWrapper.Object
            );
        }

        private void SetupBasicMocks(List<ItemPetal> itemPetals)
        {
            // Setup transform service
            foreach (var item in itemPetals)
            {
                _mockTransformService.Setup(t => t.TransformItemPetalToItemMasterSourceLog(
                    It.Is<ItemPetal>(i => i.Sku == item.Sku)))
                    .Returns(new ItemMasterSourceLog
                    {
                        Id = itemPetals.IndexOf(item) + 1,
                        Sku = item.Sku,
                        ValidationStatus = Status.Valid,
                        CreatedAt = DateTime.UtcNow
                    });

                _mockTransformService.Setup(t => t.MapToUnifiedItemMaster(
                    It.Is<ItemPetal>(i => i.Sku == item.Sku)))
                    .Returns(new UnifiedItemModel { Sku = item.Sku });
            }

            // Setup message queue
            _mockMessageQueue.Setup(m => m.PublishItemAsync(It.IsAny<UnifiedItemModel>()))
                .ReturnsAsync((true, string.Empty));

            // Setup repository
            _mockRepositoryManager.Setup(r => r.ItemMasterSourceLog.AddRange(It.IsAny<List<ItemMasterSourceLog>>()));
            _mockRepositoryManager.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);
            _mockItemMasterSourceLogRepository.Setup(repo => repo.BatchUpdateSentStatusAsync(
                It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task ProcessSkusAsync_WhenNoDataFoundInSnowflake_ReturnsErrorResponse()
        {
            // Arrange
            var skuRequest = new SkuRequestDto { Skus = new List<string> { "SKU1", "SKU2" } };

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(new List<ItemPetal>());

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.Details[0].Error.Should().Be("No data found in Snowflake");

            _mockLogger.Verify(
                l => l.LogWarning("No data found in Snowflake for the requested SKUs", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessSkusAsync_WithSpecificSkus_QueriesSnowflakeWithThoseSkus()
        {
            // Arrange
            var skus = new List<string> { "SKU1", "SKU2" };
            var skuRequest = new SkuRequestDto { Skus = skus };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU1" },
                new ItemPetal { Sku = "SKU2" }
            };

            _mockItemPetalRepository.Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            SetupBasicMocks(itemPetals);

            // Act
            await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            _mockItemPetalRepository.Verify(repo => repo.GetItemsBySkusAsync(
                It.Is<IList<string>>(list =>
                    list.Count == skus.Count &&
                    list.Contains("SKU1") &&
                    list.Contains("SKU2"))),
                Times.Once);
        }

        [Fact]
        public async Task ProcessSkusAsync_WithEmptySkuList_QueriesSnowflakeWithLimitAndHours()
        {
            // Arrange
            var skuRequest = new SkuRequestDto { Skus = new List<string>() };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU1" },
                new ItemPetal { Sku = "SKU2" }
            };

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsWithLimitAsync(
                    _schedulerConfigs.Limit,
                    _schedulerConfigs.Hours))
                .ReturnsAsync(itemPetals);

            SetupBasicMocks(itemPetals);

            // Act
            await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            _mockItemPetalRepository.Verify(repo => repo.GetItemsWithLimitAsync(
                _schedulerConfigs.Limit, _schedulerConfigs.Hours),
                Times.Once);
        }

        [Fact]
        public async Task ProcessSkusAsync_WithSmallBatchSize_UsesParallelProcessing()
        {
            // Arrange
            var skuRequest = new SkuRequestDto { Skus = new List<string> { "SKU1", "SKU2" } };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "SKU1" },
                new ItemPetal { Sku = "SKU2" }
            };

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            SetupBasicMocks(itemPetals);

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.TotalSKUs.Should().Be(2);
            result.ValidSKUs.Should().Be(2); // Assuming both items are valid
            result.LogSaved.Should().Be(2);
            result.SqsSuccess.Should().Be(2);
            result.Details.Should().HaveCount(2);

            // Verify that the database save method is called once for small batches
            // One for source logs, one for SQS results
            _mockRepositoryManager.Verify(r => r.SaveAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessSkusAsync_WithLargeBatchSize_UsesBatchProcessing()
        {
            // Arrange
            // Create SKU list larger than BatchSize
            var skus = Enumerable.Range(1, 12).Select(i => $"SKU{i}").ToList();
            var skuRequest = new SkuRequestDto { Skus = skus };

            var itemPetals = skus.Select(sku => new ItemPetal { Sku = sku }).ToList();

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            SetupBasicMocks(itemPetals);

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.TotalSKUs.Should().Be(12);
            result.ValidSKUs.Should().Be(12); // Assuming both items are valid
            result.LogSaved.Should().Be(12);
            result.SqsSuccess.Should().Be(12);
            result.Details.Should().HaveCount(12);

            // Verify if the database save method is called multiple times due to large batch
            // 6 batch * 2 operations (SaveAsync after saving source logs and after updating SQS status)
            _mockRepositoryManager.Verify(r => r.SaveAsync(), Times.AtLeast(12));
        }

        [Fact]
        public async Task ProcessSkusAsync_WithMixOfValidAndInvalidItems_HandlesAllCorrectly()
        {
            // Arrange
            var skuRequest = new SkuRequestDto
            {
                Skus = new List<string> { "Valid1", "Invalid1", "Valid2" }
            };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "Valid1" },
                new ItemPetal { Sku = "Invalid1" },
                new ItemPetal { Sku = "Valid2" }
            };

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            // Setup transform to have 2 valid items and 1 invalid item
            _mockTransformService.Setup(t => t.TransformItemPetalToItemMasterSourceLog(
                It.Is<ItemPetal>(i => i.Sku == "Valid1" || i.Sku == "Valid2")))
                .Returns((ItemPetal ip) => new ItemMasterSourceLog
                {
                    Id = ip.Sku == "Valid1" ? 1 : 3,
                    Sku = ip.Sku,
                    ValidationStatus = Status.Valid,
                    CreatedAt = DateTime.UtcNow
                });

            _mockTransformService
                .Setup(t => t.TransformItemPetalToItemMasterSourceLog(
                It.Is<ItemPetal>(i => i.Sku == "Invalid1")))
                .Returns(new ItemMasterSourceLog
                {
                    Id = 2,
                    Sku = "Invalid1",
                    ValidationStatus = Status.Invalid,
                    Errors = "Validation failed",
                    CreatedAt = DateTime.UtcNow
                });

            // Setup transform to UnifiedItemModel for valid items only
            _mockTransformService
                .Setup(t => t.MapToUnifiedItemMaster(
                It.Is<ItemPetal>(i => i.Sku == "Valid1" || i.Sku == "Valid2")))
                .Returns(
                    (ItemPetal ip) => new UnifiedItemModel
                    {
                        Sku = ip.Sku
                    }
                    );

            // Setup message queue
            _mockMessageQueue.Setup(m => m.PublishItemAsync(It.IsAny<UnifiedItemModel>()))
                .ReturnsAsync((true, string.Empty));

            // Setup repository to save source logs
            _mockRepositoryManager
                .Setup(r =>
                    r.ItemMasterSourceLog.AddRange(It.IsAny<List<ItemMasterSourceLog>>())
                    );

            _mockRepositoryManager
                .Setup(r => r.SaveAsync())
                .Returns(Task.CompletedTask);

            _mockItemMasterSourceLogRepository
                .Setup(repo => repo.BatchUpdateSentStatusAsync(
                    It.IsAny<List<int>>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.TotalSKUs.Should().Be(3);
            result.ValidSKUs.Should().Be(2);
            result.InvalidSKUs.Should().Be(1);
            result.LogSaved.Should().Be(3);
            result.SqsSuccess.Should().Be(2);
            result.SqsFailed.Should().Be(0);
        }

        [Fact]
        public async Task ProcessSkusAsync_WhenSQSPublishingFails_HandlesErrorCorrectly()
        {
            // Arrange
            var skuRequest = new SkuRequestDto { Skus = new List<string> { "Success", "Fail" } };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "Success" },
                new ItemPetal { Sku = "Fail" }
            };

            _mockItemPetalRepository
                .Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            // Setup transform service
            _mockTransformService
                .Setup(t => t.TransformItemPetalToItemMasterSourceLog(It.IsAny<ItemPetal>()))
                .Returns((ItemPetal ip) => new ItemMasterSourceLog
                {
                    Id = ip.Sku == "Success" ? 1 : 2,
                    Sku = ip.Sku,
                    ValidationStatus = Status.Valid,
                    CreatedAt = DateTime.UtcNow
                });

            _mockTransformService.Setup(t => t.MapToUnifiedItemMaster(It.IsAny<ItemPetal>()))
                .Returns((ItemPetal ip) => new UnifiedItemModel { Sku = ip.Sku });

            // Setup message queue with one successful item and one failed item
            _mockMessageQueue
                .Setup(m => m.PublishItemAsync(
                    It.Is<UnifiedItemModel>(model => model.Sku == "Success")))
                .ReturnsAsync((true, string.Empty));

            _mockMessageQueue
                .Setup(m => m.PublishItemAsync(It.Is<UnifiedItemModel>(
                model => model.Sku == "Fail")))
                .ReturnsAsync((false, "SQS error"));

            // Setup repository
            _mockRepositoryManager.Setup(r => r.ItemMasterSourceLog.AddRange(It.IsAny<List<ItemMasterSourceLog>>()));
            _mockRepositoryManager.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);

            _mockItemMasterSourceLogRepository.Setup(repo => repo.BatchUpdateSentStatusAsync(
                It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup for failed item up
            _mockItemMasterSourceLogRepository.Setup(repo => repo.FindByCondition(
                It.IsAny<System.Linq.Expressions.Expression<Func<ItemMasterSourceLog, bool>>>(),
                It.IsAny<bool>()))
                .Returns(new List<ItemMasterSourceLog>
                {
                    new ItemMasterSourceLog
                    {
                        Id = 2,
                        Sku = "Fail",
                        ValidationStatus = Status.Valid
                    }
                }.AsQueryable());

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.TotalSKUs.Should().Be(2);
            result.ValidSKUs.Should().Be(2);
            result.InvalidSKUs.Should().Be(0);
            result.LogSaved.Should().Be(2);
            result.SqsSuccess.Should().Be(1);
            result.SqsFailed.Should().Be(1);

            // Verify Detail response
            result.Details.Should().HaveCount(2);
            var failDetail = result.Details.FirstOrDefault(d => d.Sku == "Fail");
            failDetail.Should().NotBeNull();
            failDetail!.IsValid.Should().BeTrue();
            failDetail.SentToSQS.Should().BeFalse();
            failDetail.Error.Should().Be("SQS error");

            // Verify update database
            _mockItemMasterSourceLogRepository.Verify(repo => repo.Update(
                It.Is<ItemMasterSourceLog>(log =>
                    log.Id == 2 &&
                    log.Sku == "Fail" &&
                    log.IsSentToSqs == false &&
                    log.Errors == "SQS error")),
                Times.Once);
        }

        [Fact]
        public async Task ProcessSkusAsync_WhenTransformThrowsException_HandlesErrorGracefully()
        {
            // Arrange
            var skuRequest = new SkuRequestDto { Skus = new List<string> { "Normal", "Exception" } };

            var itemPetals = new List<ItemPetal>
            {
                new ItemPetal { Sku = "Normal" },
                new ItemPetal { Sku = "Exception" }
            };

            _mockItemPetalRepository.Setup(repo => repo.GetItemsBySkusAsync(It.IsAny<IList<string>>()))
                .ReturnsAsync(itemPetals);

            // Setup transform service throw exception one item
            _mockTransformService.Setup(t => t.TransformItemPetalToItemMasterSourceLog(
                It.Is<ItemPetal>(i => i.Sku == "Normal")))
                .Returns(new ItemMasterSourceLog
                {
                    Id = 1,
                    Sku = "Normal",
                    ValidationStatus = Status.Valid,
                    CreatedAt = DateTime.UtcNow
                });

            _mockTransformService.Setup(t => t.TransformItemPetalToItemMasterSourceLog(
                It.Is<ItemPetal>(i => i.Sku == "Exception")))
                .Throws(new Exception("Transform error"));

            _mockTransformService.Setup(t => t.MapToUnifiedItemMaster(
                It.Is<ItemPetal>(i => i.Sku == "Normal")))
                .Returns(new UnifiedItemModel { Sku = "Normal" });

            // Setup message queue
            _mockMessageQueue.Setup(m => m.PublishItemAsync(It.IsAny<UnifiedItemModel>()))
                .ReturnsAsync((true, string.Empty));

            // Setup repository
            _mockRepositoryManager.Setup(r => r.ItemMasterSourceLog.AddRange(It.IsAny<List<ItemMasterSourceLog>>()));
            _mockRepositoryManager.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);
            _mockItemMasterSourceLogRepository.Setup(repo => repo.BatchUpdateSentStatusAsync(
                It.IsAny<List<int>>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _skuProcessor.ProcessSkusAsync(skuRequest);

            // Assert
            result.Should().NotBeNull();
            result.TotalSKUs.Should().Be(2);
            result.ValidSKUs.Should().Be(1);
            result.InvalidSKUs.Should().Be(1);
            result.LogSaved.Should().Be(2);
            result.SqsSuccess.Should().Be(1);

            // Verify log error for exception
            _mockLogger.Verify(l => l.LogError(It.Is<string>(s =>
                s.Contains("Error processing item with SKU Exception") &&
                s.Contains("Transform error")), It.IsAny<string>()),
                Times.Once);

            // Verify detail response
            result.Details.Should().HaveCount(2);
            var exceptionDetail = result.Details.FirstOrDefault(d => d.Sku == "Exception");
            exceptionDetail.Should().NotBeNull();
            exceptionDetail!.IsValid.Should().BeFalse();
            exceptionDetail.Error.Should().Contain("Processing error: Transform error");
        }
    }
}