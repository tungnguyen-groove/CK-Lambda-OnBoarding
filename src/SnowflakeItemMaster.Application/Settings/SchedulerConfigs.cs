namespace SnowflakeItemMaster.Application.Settings
{
    public class SchedulerConfigs
    {
        public int Hours { get; set; } = 1; // Default to 1 hour
        public int Limit { get; set; } = 100; // Default to 100 items
    }
}