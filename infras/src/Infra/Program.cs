using Amazon.CDK;

namespace Infra
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new InfraStack(app, "InfraStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT") ?? "911590503922", // TODO: Research
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "ap-southeast-2" // // TODO: Research
                },
                Description = "Stack for processing SKUs from API Gateway and EventBridge"
            });
            app.Synth();
        }
    }
}