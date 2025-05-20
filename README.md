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

- .NET 6.0 SDK or later
- AWS CLI configured with appropriate permissions
- Access to Snowflake database
- MySQL RDS instance
- AWS services (Lambda, API Gateway, EventBridge, SQS)

## Project Structure

```
src/
├── SnowflakeItemMaster.Core/          # Domain models and interfaces
├── SnowflakeItemMaster.Infrastructure/ # External service implementations
├── SnowflakeItemMaster.Lambda/        # AWS Lambda function handlers
└── SnowflakeItemMaster.Api/           # API models and contracts

tests/
├── SnowflakeItemMaster.UnitTests/
└── SnowflakeItemMaster.IntegrationTests/
```

## Configuration

The service uses the following configuration structure in AWS Systems Manager Parameter Store:

```json
{
  "Snowflake": {
    "ConnectionString": "...",
    "Schema": "XB_DEV_DB",
    "Table": "ITEM_PETAL_US"
  },
  "MySQL": {
    "ConnectionString": "..."
  },
  "AWS": {
    "SQSQueueUrl": "...",
    "Region": "..."
  }
}
```

## Local Development

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   ```
3. Set up local configuration in `appsettings.Development.json`
4. Run tests:
   ```bash
   dotnet test
   ```
5. Run locally using AWS SAM:
   ```bash
   sam local start-api
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
   sam deploy --guided
   ```

## Testing

The solution includes:
- Unit tests for business logic
- Integration tests for external services
- Load tests for performance validation

Run tests with:
```bash
dotnet test
```

## Monitoring and Observability

- CloudWatch Logs for application logging
- CloudWatch Metrics for performance monitoring
- X-Ray for distributed tracing
- Custom metrics for business KPIs

## Error Handling

The service implements resilient error handling:
- Retry policies for transient failures
- Circuit breakers for external services
- Dead letter queues for failed messages
- Comprehensive error logging

## Security

- AWS IAM roles and policies
- Secrets management via AWS Systems Manager
- Encryption at rest and in transit
- Network security through VPC configuration

## Contributing

1. Create a feature branch
2. Make changes
3. Run tests
4. Submit pull request

## License

MIT License
