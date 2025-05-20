using SnowflakeItemMaster.Application.Settings;

namespace SnowflakeItemMaster.Application.Interfaces
{
    public interface IConfigWrapper
    {
        SnowflakeSettings GetSnowflakeSettings();

        DatabaseSettings GetDatabaseSettings();

        AwsSqsSettings GetAwsSqsSettings();

        PerformanceConfigs GetPerformanceConfigs();

        SchedulerConfigs GetSchedulerConfigs();
    }
}