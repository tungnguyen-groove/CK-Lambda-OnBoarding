using SnowflakeItemMaster.Application.Constansts;

namespace SnowflakeItemMaster.Application.Settings
{
    public class SnowflakeSettings
    {
        public string Account { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string ConnectionString =>
        string.Format(
            SnowflakeConstants.ConnectionStringFormat,
            Account,
            User,
            Password,
            Database,
            Schema,
            Warehouse,
            Role
        );
    }
}
