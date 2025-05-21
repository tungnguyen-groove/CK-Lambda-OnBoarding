# Snowflake Item Master Service

This AWS Lambda function service handles item master data processing from Snowflake to AWS services.

## Architecture Overview

The service follows a clean architecture pattern with the following components:

- **API Gateway Trigger**: REST API accepting SKU lists
- **EventBridge Rule**: Scheduled trigger for batch processing
- **Lambda Function**: Core business logic
- **Snowflake Integration**: Data source querying
- **SQS Queue**: Message publishing (1 message per item)
- **MySQL RDS**: Logging and audit trail
- **CloudWatch**: Observability and monitoring

## Prerequisites

- .NET 8.0 SDK
- AWS CLI configured with appropriate permissions
- Access to Snowflake database
- MySQL RDS instance
- AWS services (Lambda, API Gateway, EventBridge, SQS, RDS)

## Project Structure

```
src/
├── SnowflakeItemMaster.Core/          # Domain models and interfaces
├── SnowflakeItemMaster.Infrastructure/ # External service implementations
├── SnowflakeItemMaster.Lambda/        # AWS Lambda function handlers
└── SnowflakeItemMaster.Api/           # API models and contracts
└── SnowflakeItemMaster.Provider.Snowflake/           # Provider service implementations

tests/
├── SnowflakeItemMaster.UnitTests/
└── SnowflakeItemMaster.IntegrationTests/

infras/src
├── Infra/     # Infra stack to deploy using AWS CDK
```

## Configuration

The service uses the following configuration structure in AWS Systems Manager Parameter Store:

```json
{
   "AwsSqsSettings": {
      "QueueUrl": "your-QueueUrl",
      "Region": "your-Region",
      "MaxRetries": 3,
      "RetryDelayMilliseconds": 1000
   },
   "SnowflakeSettings": {
      "Account": "your-Account",
      "Database": "your-Database",
      "Warehouse": "your-Warehouse",
      "User": "your-User",
      "Password": "your-Password",
      "Schema": "your-Schema",
      "Role": "your-Role"
   },
   "DatabaseSettings": {
      "Host": "your-Host",
      "Database": "your-Database",
      "User": "your-User",
      "Password": "your-Password"
   },
   "PerformanceConfigs": {
      "ParallelDegree": 10,
      "BatchSize": 10,
      "EnableBatching": true
   },
   "SchedulerConfigs": {
      "Hours": 1,
      "Limit": 100
   }
}
```

## Local Development

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   ```
3. Set up local configuration in `appsettings.json`
4. Run tests:
   ```bash
   dotnet test
   ```

## Deployment

1. Build the solution:
   ```bash
   dotnet build -c Release
   ```
2. Package the Lambda:
   ```bash
   dotnet lambda package
   ```
3. Deploy using CloudFormation/SAM:
   ```bash
   cd infras
   dotnet build src
   cdk bootstrap # run only once time
   cdk synth    # Test CloudFormation template
   cdk diff     # View changes (if previously deployed)
   cdk deploy
   cdk destroy  # delete resource
   ```

## Testing

The solution includes:
- Unit tests for business logic
- Integration tests for external services

Run tests with:
```bash
dotnet test
```

## Monitoring and Observability

- CloudWatch Logs for application logging

## Error Handling

The service implements resilient error handling:
- Retry policies for transient failures
- Circuit breakers for external services
- Comprehensive error logging

## Security

- AWS IAM roles and policies

## Contributing

1. Create a feature branch
2. Make changes
3. Run tests
4. Submit pull request

## License

MIT License
