using Microsoft.Extensions.Configuration;
using SnowflakeItemMaster.Application.Constansts;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.Settings;

namespace SnowflakeItemMaster.Application.UseCases
{
    public class ConfigWrapper : IConfigWrapper
    {
        private readonly IConfiguration _configuration;

        public ConfigWrapper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SnowflakeSettings GetSnowflakeSettings()
        {
            var account = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Account);
            var user = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.User);
            var password = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Password);
            var database = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Database);
            var schema = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Schema);
            var warehouse = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Warehouse);
            var role = Environment.GetEnvironmentVariable(ConfigConstants.SnowflakeConfigConstants.Role);

            if (string.IsNullOrEmpty(account) && string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password) &&
                string.IsNullOrEmpty(database) && string.IsNullOrEmpty(schema) && string.IsNullOrEmpty(warehouse) &&
                string.IsNullOrEmpty(role))
            {
                return _configuration.GetSection("SnowflakeSettings").Get<SnowflakeSettings>() ?? new SnowflakeSettings();
            }

            return new SnowflakeSettings
            {
                Account = account ?? string.Empty,
                User = user ?? string.Empty,
                Password = password ?? string.Empty,
                Database = database ?? string.Empty,
                Schema = schema ?? string.Empty,
                Warehouse = warehouse ?? string.Empty,
                Role = role ?? string.Empty
            };
        }

        public DatabaseSettings GetDatabaseSettings()
        {
            var host = Environment.GetEnvironmentVariable(ConfigConstants.DatabaseConfigConstants.Host);
            var database = Environment.GetEnvironmentVariable(ConfigConstants.DatabaseConfigConstants.Database);
            var user = Environment.GetEnvironmentVariable(ConfigConstants.DatabaseConfigConstants.User);
            var password = Environment.GetEnvironmentVariable(ConfigConstants.DatabaseConfigConstants.Password);

            if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(database) &&
                string.IsNullOrEmpty(user) && string.IsNullOrEmpty(password))
            {
                return _configuration.GetSection("DatabaseSettings").Get<DatabaseSettings>() ?? new DatabaseSettings();
            }

            return new DatabaseSettings
            {
                Host = host ?? string.Empty,
                Database = database ?? string.Empty,
                User = user ?? string.Empty,
                Password = password ?? string.Empty
            };
        }

        public AwsSqsSettings GetAwsSqsSettings()
        {
            return _configuration.GetSection("AwsSqsSettings").Get<AwsSqsSettings>() ?? new AwsSqsSettings();
        }

        public PerformanceConfigs GetPerformanceConfigs()
        {
            return _configuration.GetSection("PerformanceConfigs").Get<PerformanceConfigs>() ?? new PerformanceConfigs();
        }

        public SchedulerConfigs GetSchedulerConfigs()
        {
            return _configuration.GetSection("SchedulerConfigs").Get<SchedulerConfigs>() ?? new SchedulerConfigs();
        }
    }
}
