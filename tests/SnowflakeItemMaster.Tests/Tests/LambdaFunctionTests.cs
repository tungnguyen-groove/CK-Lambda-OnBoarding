using System.Text.Json;
using Amazon.Lambda;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnowflakeItemMaster.Tests.Fixtures;

namespace SnowflakeItemMaster.Tests.Tests
{
    [Collection("LocalStack collection")]
    public class LambdaFunctionTests : IClassFixture<LocalStackFixture>, IDisposable
    {
        private readonly LocalStackFixture _fixture;
        private readonly IAmazonLambda _lambdaClient;
        private readonly ILogger<LambdaFunctionTests> _logger;
        private readonly HttpClient _httpClient;

        public LambdaFunctionTests(LocalStackFixture fixture)
        {
            _fixture = fixture;
            _lambdaClient = fixture.LambdaClient;
            _logger = fixture.ServiceProvider.GetRequiredService<ILogger<LambdaFunctionTests>>();
            _httpClient = new HttpClient();
            // TODO: Uncomment the following line to seed test data into Snowflake
            // Seed test data into Snowflake
            // SnowflakeTestSeeder.SeedTestData(_fixture.SnowflakeConnectionString);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        [Fact]
        public async Task ProcessSKUList_ValidInput_ShouldProcessSuccessfully()
        {
            try
            {
                // Arrange
                _logger.LogInformation("Starting test: ProcessSKUList_ValidInput_ShouldProcessSuccessfully");

                var bodyObject = new
                {
                    skus = new[]
                    {
                        "SL927177-1PTX/M/BUTTERYELLOW",
                        "WKN764-PP/ML/PALEBLUE",
                        "PH10957-PP/M/TAN",
                        "WKN764-PP/XSS/PALEBLUE",
                        "PH10957-PP/L/TAN",
                        "PH10957-PP/S/TAN",
                        "WPA255B-PP/4/SAGEGREEN",
                        "PNPEQ2251019/XS/BROWN",
                        "WPA255B-PP/8/SAGEGREEN",
                        "SPA328B-PP/20/ASHBROWN",
                        "WPA255B-PP/10/SAGEGREEN",
                        "PNPEQ3251000/L/BROWNFLORAL"
                    }
                };

                var apiEvent = new
                {
                    body = JsonSerializer.Serialize(bodyObject),
                    httpMethod = "POST",
                    path = "/skus"
                };

                _logger.LogInformation("Test data prepared: {TestData}", JsonSerializer.Serialize(apiEvent));

                // Act
                var apiUrl = $"http://localhost:4566/restapis/{_fixture.ApiId}/test/_user_request_/process-skus";
                _logger.LogInformation("Calling API Gateway endpoint: {ApiUrl}", apiUrl);

                var response = await _httpClient.PostAsync(apiUrl,
                    new StringContent(JsonSerializer.Serialize(apiEvent), System.Text.Encoding.UTF8, "application/json"));

                // Assert
                _logger.LogInformation("API Gateway response received: {StatusCode}", response.StatusCode);
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content: {Content}", responseContent);

                var responsePayload = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                responsePayload.Should().NotBeNull();
                responsePayload["statusCode"].Should().Be(200);

                _logger.LogInformation("Test completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test failed with error");
                throw;
            }
        }

    }
}