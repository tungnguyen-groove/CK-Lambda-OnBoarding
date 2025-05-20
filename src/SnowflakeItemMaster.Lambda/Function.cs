using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using SnowflakeItemMaster.Application.Dtos;
using SnowflakeItemMaster.Application.Exceptions;
using SnowflakeItemMaster.Application.Interfaces;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SnowflakeItemMaster.Lambda;

public class Function : FunctionBase
{
    private const string EventBridgeSource = "custom.skuprocessor";
    private readonly ISkuProcessor _skuProcessor;

    public Function() : base()
    {
        _skuProcessor = GetService<ISkuProcessor>();
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(object request, ILambdaContext context)
    {
        try
        {
            _logger.SetLoggerContext(context.Logger);
            _logger.LogInfo("FunctionHandler started");

            var skuRequestDto = ExtractSkuRequest(request);
            var result = await _skuProcessor.ProcessSkusAsync(skuRequestDto);

            _logger.LogInfo($"FunctionHandler completed. Processed {result.TotalSKUs} SKUs");
            return CreateSuccessResponse(result);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError($"Database connection error: {ex.Message}");
            return CreateErrorResponse(500, "Database connection error", ex.Message);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogError($"Invalid request received: {ex.Message}");
            return CreateErrorResponse(400, "Invalid request format", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error processing request: {ex}");
            return CreateErrorResponse(500, "Internal server error", ex.Message);
        }
    }

    private SkuRequestDto ExtractSkuRequest(object request)
    {
        var serializedInput = JsonSerializer.Serialize(request);
        _logger.LogInfo($"Raw request received: {serializedInput}");

        var jsonElement = JsonSerializer.Deserialize<JsonElement>(serializedInput);
        bool hasSource = TryGetPropertyIgnoreCase(jsonElement, "source", out var sourceProp);
        bool hasBody = TryGetPropertyIgnoreCase(jsonElement, "body", out var bodyProp);

        bool isEventBridge = hasSource && string.Equals(sourceProp.GetString(), EventBridgeSource, StringComparison.OrdinalIgnoreCase);
        string body = hasBody ? bodyProp.GetString() ?? string.Empty : string.Empty;
        bool isApiGateway = hasBody && !string.IsNullOrEmpty(body);

        if (isEventBridge)
        {
            _logger.LogInfo("EventBridge source detected");
            return new SkuRequestDto();
        }

        if (isApiGateway)
        {
            _logger.LogInfo("API Gateway source detected");

            try
            {
                var bodyData = JsonSerializer.Deserialize<SkuRequestDto>(body);

                if (bodyData == null)
                {
                    throw new InvalidRequestException("API Gateway request body is empty");
                }

                return new SkuRequestDto
                {
                    Skus = bodyData.Skus
                };
            }
            catch (Exception ex)
            {
                throw new InvalidRequestException($"Invalid JSON in request body: {ex.Message}");
            }
        }

        throw new InvalidRequestException("Unknown event format - request must be from EventBridge or API Gateway");
    }

    private bool TryGetPropertyIgnoreCase(JsonElement jsonElement, string propertyName, out JsonElement value)
    {
        foreach (var prop in jsonElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }
        value = default;
        return false;
    }

    private static APIGatewayProxyResponse CreateSuccessResponse(object result)
    {
        return CreateResponse(200, JsonSerializer.Serialize(new { result }));
    }

    private static APIGatewayProxyResponse CreateErrorResponse(int statusCode, string error, string details)
    {
        var errorResponse = new
        {
            error,
            result = SkuProcessingResponseDto.CreateError(details)
        };
        return CreateResponse(statusCode, JsonSerializer.Serialize(errorResponse));
    }

    private static APIGatewayProxyResponse CreateResponse(int statusCode, string body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = statusCode,
            Body = body,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "Access-Control-Allow-Origin", "*" }
            }
        };
    }
}