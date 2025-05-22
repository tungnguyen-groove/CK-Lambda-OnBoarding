using System.Runtime.InteropServices;
using Amazon.APIGateway;
using Amazon.APIGateway.Model;
using Amazon.EventBridge;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnowflakeItemMaster.Tests.Helpers;
using Environment = System.Environment;

namespace SnowflakeItemMaster.Tests.Fixtures
{
    public class LocalStackFixture : IAsyncLifetime
    {
        private readonly IContainer _localStackContainer;
        private const string LocalStackEndpoint = "http://localhost:4566";
        private const string Region = "ap-southeast-2";
        private readonly ILogger<LocalStackFixture> _logger;
        private readonly string _lambdaProjectPath;

        // Fake credentials for LocalStack
        private const string FakeAccessKey = "test";
        private const string FakeSecretKey = "test";

        public IAmazonLambda LambdaClient { get; private set; }
        public IAmazonSQS SQSClient { get; private set; }
        public IAmazonEventBridge EventBridgeClient { get; private set; }
        public IAmazonAPIGateway APIGatewayClient { get; private set; }
        public IServiceProvider ServiceProvider { get; private set; }

        public string QueueUrl { get; private set; }
        public string ApiId { get; private set; }
        public string LambdaFunctionName { get; private set; }
        public string SnowflakeConnectionString { get; }

        public LocalStackFixture()
        {
            try
            {
                // IMPORTANT: Override AWS environment variables FIRST to prevent SDK from using real AWS credentials
                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", FakeAccessKey);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", FakeSecretKey);
                Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", Region);
                Environment.SetEnvironmentVariable("AWS_ENDPOINT_URL", LocalStackEndpoint);

                // Clear any existing AWS profile to prevent conflicts
                Environment.SetEnvironmentVariable("AWS_PROFILE", "");
                Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", "");
                Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", "");

                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                _logger = ServiceProvider.GetRequiredService<ILogger<LocalStackFixture>>();

                // Get Lambda project path
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _lambdaProjectPath = Path.GetFullPath(Path.Combine(
                    baseDirectory,
                    "..", "..", "..", "..", "..",
                    "src", "SnowflakeItemMaster.Lambda", "SnowflakeItemMaster.Lambda.csproj"
                ));

                if (!File.Exists(_lambdaProjectPath))
                {
                    throw new FileNotFoundException($"Lambda project file not found at: {_lambdaProjectPath}");
                }

                // Read connection string from appsettings.test.json
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.test.json", optional: true)
                    .Build();
                SnowflakeConnectionString = config["Snowflake:ConnectionString"] ?? string.Empty;

                var containerBuilder = new ContainerBuilder()
                    .WithImage("localstack/localstack:latest")
                    .WithEnvironment("SERVICES", "lambda,sqs,apigateway,events,rds")
                    .WithEnvironment("DEFAULT_REGION", Region)
                    .WithEnvironment("AWS_DEFAULT_REGION", Region)
                    .WithEnvironment("AWS_ACCESS_KEY_ID", FakeAccessKey)
                    .WithEnvironment("AWS_SECRET_ACCESS_KEY", FakeSecretKey)
                    .WithEnvironment("DEBUG", "1")
                    .WithEnvironment("LAMBDA_EXECUTOR", "docker")
                    .WithEnvironment("LAMBDA_RUNTIME_EXECUTOR", "docker")
                    .WithPortBinding(4566, 4566)
                    .WithWaitStrategy(Wait.ForUnixContainer()
                        .UntilPortIsAvailable(4566));

                // Add Docker socket path based on OS
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    containerBuilder.WithEnvironment("DOCKER_HOST", "tcp://host.docker.internal:2375");
                }
                else
                {
                    containerBuilder.WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock");
                }

                _localStackContainer = containerBuilder.Build();

                // Create explicit credentials for LocalStack with fallback chain disabled
                var credentials = new BasicAWSCredentials(FakeAccessKey, FakeSecretKey);

                // Configure clients with explicit credentials and LocalStack endpoint
                var lambdaConfig = new Amazon.Lambda.AmazonLambdaConfig
                {
                    ServiceURL = LocalStackEndpoint,
                    UseHttp = true,
                    MaxErrorRetry = 3,
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(Region),
                    DisableLogging = false // Enable for debugging
                };

                var sqsConfig = new Amazon.SQS.AmazonSQSConfig
                {
                    ServiceURL = LocalStackEndpoint,
                    UseHttp = true,
                    MaxErrorRetry = 3,
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(Region),
                    DisableLogging = false
                };

                var eventBridgeConfig = new Amazon.EventBridge.AmazonEventBridgeConfig
                {
                    ServiceURL = LocalStackEndpoint,
                    UseHttp = true,
                    MaxErrorRetry = 3,
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(Region),
                    DisableLogging = false
                };

                var apiGatewayConfig = new Amazon.APIGateway.AmazonAPIGatewayConfig
                {
                    ServiceURL = LocalStackEndpoint,
                    UseHttp = true,
                    MaxErrorRetry = 3,
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(Region),
                    DisableLogging = false
                };

                // Initialize clients with explicit credentials
                LambdaClient = new AmazonLambdaClient(credentials, lambdaConfig);
                SQSClient = new AmazonSQSClient(credentials, sqsConfig);
                EventBridgeClient = new AmazonEventBridgeClient(credentials, eventBridgeConfig);
                APIGatewayClient = new AmazonAPIGatewayClient(credentials, apiGatewayConfig);

                _logger.LogInformation("LocalStack clients initialized with fake credentials");
                _logger.LogInformation("SQS Client Endpoint: {Endpoint}", SQSClient.Config.ServiceURL);
                _logger.LogInformation("Lambda Client Endpoint: {Endpoint}", LambdaClient.Config.ServiceURL);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing LocalStackFixture");
                throw;
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting LocalStack container...");
                await _localStackContainer.StartAsync();
                _logger.LogInformation("LocalStack container started successfully");

                // Wait longer for LocalStack to be fully ready
                _logger.LogInformation("Waiting for LocalStack to be ready...");
                await Task.Delay(10000); // Increase to 10 seconds

                // Verify LocalStack is responding with retry logic
                await VerifyLocalStackConnectionWithRetry();

                _logger.LogInformation("Setting up LocalStack resources...");
                await SetupLocalStackResources();
                _logger.LogInformation("LocalStack resources setup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during LocalStack initialization");
                throw;
            }
        }

        private async Task VerifyLocalStackConnectionWithRetry()
        {
            const int maxRetries = 5;
            const int delayMs = 3000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Verifying LocalStack connection (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);

                    // Try to list SQS queues to verify connection
                    var listQueuesRequest = new ListQueuesRequest();
                    var listQueuesResponse = await SQSClient.ListQueuesAsync(listQueuesRequest);
                    _logger.LogInformation("LocalStack connection verified. Found {QueueCount} existing queues",
                        listQueuesResponse.QueueUrls?.Count ?? 0);
                    return; // Success, exit retry loop
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LocalStack connection attempt {Attempt} failed: {Message}", attempt, ex.Message);

                    if (attempt == maxRetries)
                    {
                        _logger.LogError("Failed to verify LocalStack connection after {MaxRetries} attempts", maxRetries);
                        throw;
                    }

                    await Task.Delay(delayMs);
                }
            }
        }

        private async Task SetupLocalStackResources()
        {
            try
            {
                // Create SQS Queue
                _logger.LogInformation("Creating SQS queue...");
                var createQueueResponse = await SQSClient.CreateQueueAsync(new CreateQueueRequest
                {
                    QueueName = "item-master-queue",
                    Attributes = new Dictionary<string, string>
                    {
                        { "VisibilityTimeout", "30" },
                        { "MessageRetentionPeriod", "345600" }
                    }
                });
                QueueUrl = createQueueResponse.QueueUrl;
                _logger.LogInformation("SQS queue created: {QueueUrl}", QueueUrl);

                // Build and deploy Lambda function
                _logger.LogInformation("Building Lambda function package...");
                var lambdaPackage = await LambdaBuilder.BuildLambdaPackage(_lambdaProjectPath, _logger);

                // Create Lambda function
                LambdaFunctionName = "item-master-function";
                _logger.LogInformation("Creating Lambda function: {FunctionName}", LambdaFunctionName);

                var createFunctionRequest = new CreateFunctionRequest
                {
                    FunctionName = LambdaFunctionName,
                    Runtime = "dotnet8",
                    Handler = "SnowflakeItemMaster.Lambda::SnowflakeItemMaster.Lambda.Function::FunctionHandler",
                    Role = "arn:aws:iam::000000000000:role/lambda-role",
                    Code = new FunctionCode
                    {
                        ZipFile = new MemoryStream(lambdaPackage)
                    },
                    Environment = new Amazon.Lambda.Model.Environment
                    {
                        Variables = new Dictionary<string, string>
                        {
                            //{ "SNOWFLAKE_ACCOUNT", "test-account" },
                            //{ "SNOWFLAKE_DATABASE", "TEST_DB" },
                            //{ "SNOWFLAKE_WAREHOUSE", "TEST_WH" },
                            //{ "SNOWFLAKE_USERNAME", "test-user" },
                            //{ "SNOWFLAKE_PASSWORD", "test-password" },
                            //{ "SNOWFLAKE_SCHEMA", "TEST_SCHEMA" },
                            //{ "SNOWFLAKE_ROLE", "TEST_ROLE" }
                            { "AWS_SQS_QueueURL", QueueUrl },
                            { "AWS_SQS_QueueURL", Region }
                        }
                    },
                    MemorySize = 512,
                    Timeout = 30
                };

                var createFunctionResponse = await LambdaClient.CreateFunctionAsync(createFunctionRequest);
                _logger.LogInformation("Lambda function created: {FunctionArn}", createFunctionResponse.FunctionArn);

                // Create API Gateway
                _logger.LogInformation("Creating API Gateway...");
                var createRestApiResponse = await APIGatewayClient.CreateRestApiAsync(new CreateRestApiRequest
                {
                    Name = "item-master-api",
                    Description = "API for Item Master Integration Tests"
                });
                ApiId = createRestApiResponse.Id;
                _logger.LogInformation("API Gateway created: {ApiId}", ApiId);

                // Configure API Gateway
                var resources = await APIGatewayClient.GetResourcesAsync(new GetResourcesRequest { RestApiId = ApiId });
                var rootResource = resources.Items.First(r => r.Path == "/");

                // Create resource for Lambda integration
                var createResourceResponse = await APIGatewayClient.CreateResourceAsync(new CreateResourceRequest
                {
                    RestApiId = ApiId,
                    ParentId = rootResource.Id,
                    PathPart = "process-skus"
                });

                // Create POST method
                await APIGatewayClient.PutMethodAsync(new PutMethodRequest
                {
                    RestApiId = ApiId,
                    ResourceId = createResourceResponse.Id,
                    HttpMethod = "POST",
                    AuthorizationType = "NONE"
                });

                // Create Lambda integration
                await APIGatewayClient.PutIntegrationAsync(new PutIntegrationRequest
                {
                    RestApiId = ApiId,
                    ResourceId = createResourceResponse.Id,
                    HttpMethod = "POST",
                    Type = IntegrationType.AWS_PROXY,
                    IntegrationHttpMethod = "POST",
                    Uri = $"arn:aws:apigateway:{Region}:lambda:path/2015-03-31/functions/{createFunctionResponse.FunctionArn}/invocations"
                });

                // Deploy API
                var deployment = await APIGatewayClient.CreateDeploymentAsync(new CreateDeploymentRequest
                {
                    RestApiId = ApiId,
                    StageName = "test"
                });

                _logger.LogInformation("API Gateway configured and deployed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up LocalStack resources");
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            try
            {
                _logger.LogInformation("Disposing LocalStack container...");
                await _localStackContainer.DisposeAsync();
                _logger.LogInformation("LocalStack container disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing LocalStack container");
                throw;
            }
        }
    }
}