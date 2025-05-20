using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Polly;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Contracts.MessageQueue;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Application.Settings;

namespace SnowflakeItemMaster.Infrastructure.Services
{
    public class AwsSqsMessageQueueService : IMessageQueueService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly AwsSqsSettings _settings;
        private readonly ILoggingService _logger;
        private readonly IAsyncPolicy _retryPolicy;
        private const string DefaultGroupId = "default-group";

        public AwsSqsMessageQueueService(
            IAmazonSQS sqsClient,
            ILoggingService logger,
            IConfigWrapper configWrapper)
        {
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _settings = configWrapper.GetAwsSqsSettings() ?? throw new ArgumentNullException(nameof(_settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure retry policy using Polly
            _retryPolicy = Policy
                .Handle<AmazonSQSException>()
                .WaitAndRetryAsync(
                    _settings.MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(_settings.RetryDelayMilliseconds * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} of {_settings.MaxRetries} publishing message to SQS after {timeSpan.TotalSeconds:n1}s delay. Error: {exception.Message}", "Retry");
                    }
                );
        }

        public async Task<(bool IsSuccess, string Message)> PublishItemAsync(UnifiedItemModel item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try
            {
                var messageBody = JsonSerializer.Serialize(item);
                var request = new SendMessageRequest
                {
                    QueueUrl = _settings.QueueUrl,
                    MessageBody = messageBody,
                    MessageGroupId = DefaultGroupId,
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>
                           {
                               {
                                   "MessageType",
                                   new MessageAttributeValue
                                   {
                                       DataType = "String",
                                       StringValue = "ItemMaster"
                                   }
                               },
                               {
                                   "Timestamp",
                                   new MessageAttributeValue
                                   {
                                       DataType = "String",
                                       StringValue = DateTime.UtcNow.ToString("O")
                                   }
                               }
                           }
                };

                var response = await _retryPolicy.ExecuteAsync(async () =>
                    await _sqsClient.SendMessageAsync(request)
                );

                _logger.LogInfo($"Successfully published message for SKU {item.Sku} to SQS. MessageId: {response.MessageId}");
                return (true, $"Message published successfully. MessageId: {response.MessageId}");
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to publish message for SKU {item.Sku} to SQS. Error: {ex.Message}";
                _logger.LogError(errorMessage);
                return (false, errorMessage);
            }
        }
    }
}