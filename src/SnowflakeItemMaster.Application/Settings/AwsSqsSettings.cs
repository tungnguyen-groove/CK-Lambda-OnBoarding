namespace SnowflakeItemMaster.Application.Settings
{
    public class AwsSqsSettings
    {
        public string QueueUrl { get; set; }
        public string Region { get; set; }
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
    }
}
