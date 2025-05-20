using SnowflakeItemMaster.Application.Constansts;

namespace SnowflakeItemMaster.Application.Settings
{
    public class DatabaseSettings
    {
        public string Host { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string ConnectionString =>
        string.Format(
            DatabaseConstants.ConnectionStringFormat,
            Host,
            Database,
            User,
            Password
        );
    }
}
