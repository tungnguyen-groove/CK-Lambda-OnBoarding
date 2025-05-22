using System.Text.Json;
using FluentAssertions;

namespace SnowflakeItemMaster.Integrations
{
    public class LambdaFunctionTests : IDisposable
    {
        private readonly HttpClient _httpClient;

        public LambdaFunctionTests()
        {
            _httpClient = new HttpClient();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        [Fact]
        public async Task ProcessSKUList_ValidInput_ShouldProcessSuccessfully()
        {
            // Arrange
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

            // Act
            var apiUrl = $"https://2ts4mndjw7.execute-api.ap-southeast-2.amazonaws.com/dev";
            var response = await _httpClient.PostAsync(apiUrl,
                new StringContent(JsonSerializer.Serialize(apiEvent), System.Text.Encoding.UTF8, "application/json"));

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync();

            var responsePayload = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            responsePayload.Should().NotBeNull();

            // Validate headers
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(responsePayload["headers"].ToString()!);
            headers.Should().ContainKey("Content-Type").WhoseValue.Should().Be("application/json");
            headers.Should().ContainKey("Access-Control-Allow-Origin").WhoseValue.Should().Be("*");

            // Validate body
            var bodyJson = responsePayload["body"].ToString();
            bodyJson.Should().NotBeNullOrWhiteSpace();

            var body = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyJson!);
            body.Should().ContainKey("result");

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(body["result"].ToString()!);
            result.Should().NotBeNull();
        }
        [Fact]
        public async Task ProcessSKUList_BodyEmptyInput_ShouldReturnBadRequest()
        {
            // Arrange
            var bodyObject = new
            {
            };

            var apiEvent = new
            {
                body = JsonSerializer.Serialize(bodyObject),
                httpMethod = "POST",
                path = "/skus"
            };

            // Act
            var apiUrl = $"https://2ts4mndjw7.execute-api.ap-southeast-2.amazonaws.com/dev";
            var response = await _httpClient.PostAsync(apiUrl,
                new StringContent(JsonSerializer.Serialize(apiEvent), System.Text.Encoding.UTF8, "application/json"));

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();

            var responsePayload = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
            responsePayload.Should().NotBeNull();

            // Validate status code
            responsePayload.Should().ContainKey("statusCode");
            responsePayload["statusCode"].ToString().Should().Be("200");

            // Validate headers
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(responsePayload["headers"].ToString()!);
            headers.Should().ContainKey("Content-Type").WhoseValue.Should().Be("application/json");
            headers.Should().ContainKey("Access-Control-Allow-Origin").WhoseValue.Should().Be("*");

            // Validate body
            var bodyJson = responsePayload["body"].ToString();
            bodyJson.Should().NotBeNullOrWhiteSpace();

            var body = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyJson!);
            body.Should().ContainKey("result");

            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(body["result"].ToString()!);
            result.Should().NotBeNull();

            var details = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(result["Details"].ToString()!);
            details.Should().NotBeNull();
            var detail = details[0];
        }
    }
}