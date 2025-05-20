namespace SnowflakeItemMaster.Application.Settings
{
    public class PerformanceConfigs
    {
        public int ParallelDegree { get; set; } = 100;
        public int BatchSize { get; set; } = 100;
        public bool EnableBatching { get; set; } = true;
    }
}