using Amazon.SQS;
using Amazon.SQS.Model;
using FluentAssertions;
using Moq;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Models;
using SnowflakeItemMaster.Application.Settings;
using SnowflakeItemMaster.Infrastructure.Services;

namespace SnowflakeItemMaster.Infrastructure.Tests.Services
{
    public class AwsSqsMessageQueueServiceTests
    {
        private readonly Mock<IAmazonSQS> _mockSqsClient;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly Mock<IConfigWrapper> _mockConfigWrapper;
        private readonly AwsSqsSettings _awsSqsSettings;
        private readonly AwsSqsMessageQueueService _service;

        public AwsSqsMessageQueueServiceTests()
        {
            _mockSqsClient = new Mock<IAmazonSQS>();
            _mockLogger = new Mock<ILoggingService>();
            _mockConfigWrapper = new Mock<IConfigWrapper>();

            _awsSqsSettings = new AwsSqsSettings
            {
                QueueUrl = "https://sqs.region.amazonaws.com/123456789012/test-queue.fifo",
                MaxRetries = 3,
                RetryDelayMilliseconds = 100
            };

            // Setup mock config wrapper để trả về settings
            _mockConfigWrapper.Setup(c => c.GetAwsSqsSettings()).Returns(_awsSqsSettings);

            _service = new AwsSqsMessageQueueService(
                _mockSqsClient.Object,
                _mockLogger.Object,
                _mockConfigWrapper.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullSqsClient_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new AwsSqsMessageQueueService(
                null,
                _mockLogger.Object,
                _mockConfigWrapper.Object);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("sqsClient");
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Action act = () => new AwsSqsMessageQueueService(
                _mockSqsClient.Object,
                null,
                _mockConfigWrapper.Object);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_NullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            _mockConfigWrapper.Setup(c => c.GetAwsSqsSettings()).Returns((AwsSqsSettings)null);

            // Act & Assert
            Action act = () => new AwsSqsMessageQueueService(
                _mockSqsClient.Object,
                _mockLogger.Object,
                _mockConfigWrapper.Object);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public async Task PublishItemAsync_NullItem_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Func<Task> act = async () => await _service.PublishItemAsync(null);

            await act.Should().ThrowAsync<ArgumentNullException>()
                .WithParameterName("item");
        }

        #endregion Constructor Tests

        #region PublishItemAsync

        [Fact]
        public async Task PublishItemAsync_ValidItem_PublishesSuccessfully()
        {
            // Arrange
            var item = new UnifiedItemModel { Sku = "TEST-123" };
            var expectedMessageId = "msg-123456";

            _mockSqsClient.Setup(m => m.SendMessageAsync(
                    It.IsAny<SendMessageRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SendMessageResponse { MessageId = expectedMessageId });

            // Act
            var result = await _service.PublishItemAsync(item);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain(expectedMessageId);

            _mockSqsClient.Verify(m => m.SendMessageAsync(
                It.Is<SendMessageRequest>(req =>
                    req.QueueUrl == _awsSqsSettings.QueueUrl &&
                    req.MessageGroupId == "default-group" &&
                    req.MessageAttributes.ContainsKey("MessageType") &&
                    req.MessageAttributes.ContainsKey("Timestamp") &&
                    req.MessageBody.Contains(item.Sku)),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _mockLogger.Verify(l => l.LogInfo(It.Is<string>(s =>
                s.Contains("Successfully published") &&
                s.Contains(item.Sku) &&
                s.Contains(expectedMessageId)), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishItemAsync_SqsClientThrowsException_ReturnsFailure()
        {
            // Arrange
            var item = new UnifiedItemModel { Sku = "TEST-123" };
            var expectedException = new AmazonSQSException("Test exception");

            _mockSqsClient.Setup(m => m.SendMessageAsync(
                    It.IsAny<SendMessageRequest>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _service.PublishItemAsync(item);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("Failed to publish message");
            result.Message.Should().Contain(item.Sku);
            result.Message.Should().Contain(expectedException.Message);

            _mockLogger.Verify(l => l.LogError(It.Is<string>(s =>
                s.Contains("Failed to publish message") &&
                s.Contains(item.Sku) &&
                s.Contains(expectedException.Message)), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishItemAsync_SqsClientThrowsAndRetries_EventuallySucceeds()
        {
            // Arrange
            var item = new UnifiedItemModel { Sku = "TEST-123" };
            var expectedMessageId = "msg-123456";
            var expectedException = new AmazonSQSException("Transient exception");

            var callCount = 0;
            _mockSqsClient.Setup(m => m.SendMessageAsync(
                    It.IsAny<SendMessageRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw expectedException;
                    }
                    return new SendMessageResponse { MessageId = expectedMessageId };
                });

            // Act
            var result = await _service.PublishItemAsync(item);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Message.Should().Contain(expectedMessageId);

            _mockSqsClient.Verify(m => m.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(2));

            _mockLogger.Verify(l => l.LogWarning(
                It.Is<string>(s => s.Contains("Retry 1 of") && s.Contains(expectedException.Message)),
                "Retry"),
                Times.Once);

            _mockLogger.Verify(l => l.LogInfo(It.Is<string>(s =>
                s.Contains("Successfully published") &&
                s.Contains(item.Sku) &&
                s.Contains(expectedMessageId)), It.IsAny<string>()),
                Times.Once);
        }

        #endregion PublishItemAsync
    }
}