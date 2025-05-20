using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;
using FluentAssertions;
using Moq;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Dtos;
using SnowflakeItemMaster.Application.Exceptions;
using SnowflakeItemMaster.Application.Interfaces;

namespace SnowflakeItemMaster.Lambda.Tests
{
    public class FunctionTests
    {
        private readonly Mock<ISkuProcessor> _mockSkuProcessor;
        private readonly Mock<ILoggingService> _mockLogger;
        private readonly TestFunction _function;
        private readonly TestLambdaContext _lambdaContext;

        public FunctionTests()
        {
            _mockSkuProcessor = new Mock<ISkuProcessor>();
            _mockLogger = new Mock<ILoggingService>();
            _lambdaContext = new TestLambdaContext();
            _function = new TestFunction(_mockSkuProcessor.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task FunctionHandler_WithValidApiGatewayRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var skus = new List<string> { "SKU1", "SKU2" };
            var skuRequestDto = new SkuRequestDto { Skus = skus };
            var bodyContent = JsonSerializer.Serialize(skuRequestDto);

            var request = new APIGatewayProxyRequest
            {
                Body = bodyContent
            };

            var expectedResponse = new SkuProcessingResponseDto
            {
                TotalSKUs = 2,
                ValidSKUs = 2,
                InvalidSKUs = 0,
                SqsSuccess = 2,
                SqsFailed = 0,
                LogSaved = 2,
            };

            _mockSkuProcessor.Setup(x => x.ProcessSkusAsync(It.IsAny<SkuRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _function.FunctionHandler(request, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(200);
            response.Headers["Content-Type"].Should().Be("application/json");
            response.Headers["Access-Control-Allow-Origin"].Should().Be("*");

            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);
            _mockSkuProcessor.Verify(x => x.ProcessSkusAsync(It.Is<SkuRequestDto>(dto =>
                dto.Skus.Count == 2 &&
                dto.Skus.Contains("SKU1") &&
                dto.Skus.Contains("SKU2"))), Times.Once);
            _mockLogger.Verify(x => x.LogInfo(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task FunctionHandler_WithValidEventBridgeRequest_ReturnsSuccessResponse()
        {
            // Arrange
            var eventBridgeRequest = new
            {
                source = "custom.skuprocessor",
            };

            var expectedResponse = new SkuProcessingResponseDto
            {
                TotalSKUs = 2,
                ValidSKUs = 2,
                InvalidSKUs = 0,
                SqsSuccess = 2,
                SqsFailed = 0,
                LogSaved = 2,
            };

            _mockSkuProcessor.Setup(x => x.ProcessSkusAsync(It.IsAny<SkuRequestDto>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await _function.FunctionHandler(eventBridgeRequest, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(200);
            _mockSkuProcessor.Verify(x => x.ProcessSkusAsync(It.Is<SkuRequestDto>(dto =>
                dto.Skus.Count == 0)), Times.Once);
            _mockLogger.Verify(x => x.LogInfo("EventBridge source detected", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FunctionHandler_WithInvalidRequest_Returns400Response()
        {
            // Arrange
            var invalidRequest = new APIGatewayProxyRequest
            {
                Body = "invalid json {"
            };

            // Act
            var response = await _function.FunctionHandler(invalidRequest, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(400);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);
            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Invalid JSON in request body")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FunctionHandler_WithEmptyBody_Returns400Response()
        {
            // Arrange
            var emptyRequest = new APIGatewayProxyRequest
            {
                Body = ""
            };

            // Act
            var response = await _function.FunctionHandler(emptyRequest, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(400);
            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Invalid request received")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FunctionHandler_WithRepositoryException_Returns500Response()
        {
            // Arrange
            var skus = new List<string> { "SKU1", "SKU2" };
            var skuRequestDto = new SkuRequestDto { Skus = skus };
            var bodyContent = JsonSerializer.Serialize(skuRequestDto);

            var request = new APIGatewayProxyRequest
            {
                Body = bodyContent
            };

            _mockSkuProcessor.Setup(x => x.ProcessSkusAsync(It.IsAny<SkuRequestDto>()))
                .ThrowsAsync(new RepositoryException("Database connection failed"));

            // Act
            var response = await _function.FunctionHandler(request, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(500);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);
            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Database connection error")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FunctionHandler_WithUnexpectedException_Returns500Response()
        {
            // Arrange
            var skus = new List<string> { "SKU1", "SKU2" };
            var skuRequestDto = new SkuRequestDto { Skus = skus };
            var bodyContent = JsonSerializer.Serialize(skuRequestDto);

            var request = new APIGatewayProxyRequest
            {
                Body = bodyContent
            };

            _mockSkuProcessor.Setup(x => x.ProcessSkusAsync(It.IsAny<SkuRequestDto>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var response = await _function.FunctionHandler(request, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(500);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);
            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Unexpected error processing request")), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task FunctionHandler_WithUnknownEventFormat_Returns400Response()
        {
            // Arrange
            var unknownRequest = new { unknownProperty = "value" };

            // Act
            var response = await _function.FunctionHandler(unknownRequest, _lambdaContext);

            // Assert
            response.StatusCode.Should().Be(400);
            var responseBody = JsonSerializer.Deserialize<dynamic>(response.Body);
            _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Invalid request received")), It.IsAny<string>()), Times.Once);
        }
    }
}

