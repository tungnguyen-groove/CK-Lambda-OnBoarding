# Snowflake Item Master Integration Tests

This project contains integration tests for the Snowflake Item Master AWS Lambda project. The tests use LocalStack to simulate AWS services locally.

## Prerequisites

- .NET 8.0 SDK
- Docker Desktop for Windows
- AWS CLI (for local development)
- Visual Studio 2022 or VS Code

## Setup LocalStack

Install Docker Desktop for Windows if not already installed

## Project Structure

```
SnowflakeItemMaster.Integration.Tests/
├── Fixtures/
│   └── LocalStackFixture.cs        # LocalStack container management
├── Helpers/
│   ├── LambdaBuilder.cs            # AWS service helper methods
│   └── SnowflakeTestSeeder.cs      # Test data Snowflake
└── Tests/
    └── LambdaFunctionTests.cs      # Lambda function integration tests
```

## Running Tests

Run tests using Visual Studio Test Explorer or command line:

```powershell
dotnet test --logger "console;verbosity=detailed"
```