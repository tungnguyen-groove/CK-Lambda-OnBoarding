using System.Data;
using Snowflake.Data.Client;
using SnowflakeItemMaster.Application.Contracts.DatabaseConnection;
using SnowflakeItemMaster.Application.Exceptions;
using SnowflakeItemMaster.Application.Interfaces;

namespace SnowflakeItemMaster.Provider.Snowflake
{
    public class SnowflakeConnectionFactory : IDatabaseConnectionFactory
    {
        private readonly string _connectionString;

        public SnowflakeConnectionFactory(IConfigWrapper configWrapper)
        {
            _connectionString = configWrapper.GetSnowflakeSettings()?.ConnectionString ?? throw new ArgumentNullException(nameof(_connectionString));
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            try
            {
                var connection = new SnowflakeDbConnection
                {
                    ConnectionString = _connectionString
                };
                await connection.OpenAsync();
                return connection;
            }
            catch (Exception ex)
            {
                throw new RepositoryException("Failed to create database connection", ex);
            }
        }
    }
}