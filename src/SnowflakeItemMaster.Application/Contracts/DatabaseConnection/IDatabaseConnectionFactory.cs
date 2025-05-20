using System.Data;

namespace SnowflakeItemMaster.Application.Contracts.DatabaseConnection
{
    public interface IDatabaseConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
}