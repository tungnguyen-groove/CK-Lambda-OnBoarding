using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using Infra.Constants;
using System.Collections.Generic;

namespace Infra
{
    public class InfraStack : Stack
    {
        internal InfraStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            #region Create IAM role for Lambda

            var lambdaRole = new Role(this, "SnowflakeItemMasterLambdaRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    // ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambda_FullAccess"),
                    //ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonSQSFullAccess")
                }
            });

            #endregion Create IAM role for Lambda

            #region Create Lambda function using existing SnowflakeItemMaster.Lambda

            // Create Lambda function using existing SnowflakeItemMaster.Lambda
            var itemMasterFunction = new Function(this, "SnowflakeItemMasterFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Handler = "SnowflakeItemMaster.Lambda::SnowflakeItemMaster.Lambda.Function::FunctionHandler",
                FunctionName = "SnowflakeItemMasterFunction",
                Code = Code.FromAsset("../src/SnowflakeItemMaster.Lambda/bin/Release/net8.0/publish"),
                Role = lambdaRole,
                Timeout = Duration.Seconds(30),
                MemorySize = 256,
                Environment = new Dictionary<string, string>
                {
                    // Environment variables for Snowflake connection
                    { SnowflakeConstants.Account, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Account) ?? "" },
                    { SnowflakeConstants.User, System.Environment.GetEnvironmentVariable(SnowflakeConstants.User) ?? "" },
                    { SnowflakeConstants.Password, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Password) ?? "" },
                    { SnowflakeConstants.Warehouse, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Warehouse) ?? "" },
                    { SnowflakeConstants.Database, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Database) ?? "" },
                    { SnowflakeConstants.Schema, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Schema) ?? "" },
                    { SnowflakeConstants.Role, System.Environment.GetEnvironmentVariable(SnowflakeConstants.Role) ?? "" },

                    // Environment variables for Database connection
                    { DatabaseConstants.Host, System.Environment.GetEnvironmentVariable(DatabaseConstants.Host) ?? "" },
                    { DatabaseConstants.Database, System.Environment.GetEnvironmentVariable(DatabaseConstants.Database) ?? "" },
                    { DatabaseConstants.User, System.Environment.GetEnvironmentVariable(DatabaseConstants.User) ?? "" },
                    { DatabaseConstants.Password, System.Environment.GetEnvironmentVariable(DatabaseConstants.Password) ?? "" },
                }
            });

            #endregion Create Lambda function using existing SnowflakeItemMaster.Lambda

            #region Create API Gateway REST API

            // Create API Gateway REST API
            var api = new RestApi(this, "ItemMasterApi", new RestApiProps
            {
                RestApiName = "Snowflake Item Master API",
                Description = "API for processing SKUs in Snowflake",
                DeployOptions = new StageOptions
                {
                    StageName = "dev"
                }
            });

            // Create resource for the API Gateway
            // Add POST /skus endpoint
            var skusResource = api.Root.AddResource("skus");
            skusResource.AddMethod("POST", new LambdaIntegration(itemMasterFunction), new MethodOptions
            {
                ApiKeyRequired = false,
            });

            // Output the API URL
            new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Description = "API Gateway URL",
                Value = api.Url
            });

            #endregion Create API Gateway REST API

            #region Create EventBridge Rule

            // Create EventBridge Rule
            var eventRule = new Rule(this, "ItemMasterRule", new RuleProps
            {
                RuleName = "ItemMasterRule",
                Description = "Rule to trigger Lambda function for SKU processing",
                EventPattern = new EventPattern
                {
                    Source = new[] { "custom.skuprocessor" },
                    DetailType = new[] { "SKUProcessing" },
                }
            });

            // Add Lambda as target for the EventBridge rule
            eventRule.AddTarget(new LambdaFunction(itemMasterFunction));

            // Add additional permissions for EventBridge to invoke Lambda
            itemMasterFunction.AddPermission("EventBridgeInvokePermission", new Permission
            {
                Principal = new ServicePrincipal("events.amazonaws.com"),
                Action = "lambda:InvokeFunction",
                SourceArn = eventRule.RuleArn
            });

            #endregion Create EventBridge Rule

            #region Create EventBridge Schedule Rule - Runs every 10 minutes

            // Create EventBridge Schedule Rule to run every 10 minutes
            var scheduleRule = new Rule(this, "ItemMasterScheduleRule", new RuleProps
            {
                RuleName = "ItemMasterScheduleRule",
                Description = "Schedule rule to trigger Lambda function every 10 minutes",
                Schedule = Schedule.Rate(Duration.Minutes(10))
            });

            // Add Lambda as target for the schedule rule
            scheduleRule.AddTarget(new LambdaFunction(itemMasterFunction, new LambdaFunctionProps
            {
                Event = RuleTargetInput.FromObject(new Dictionary<string, object>
                {
                    { "source", "scheduled" },
                    { "action", "process_pending_skus" }
                })
            }));

            // Add additional permissions for schedule rule to invoke Lambda
            itemMasterFunction.AddPermission("ScheduleInvokePermission", new Permission
            {
                Principal = new ServicePrincipal("events.amazonaws.com"),
                Action = "lambda:InvokeFunction",
                SourceArn = scheduleRule.RuleArn
            });

            #endregion Create EventBridge Schedule Rule - Runs every 10 minutes
        }
    }
}