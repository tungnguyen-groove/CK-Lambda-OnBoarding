using Dapper;
using SnowflakeItemMaster.Application.Contracts.DatabaseConnection;
using SnowflakeItemMaster.Application.Contracts.Provider;
using SnowflakeItemMaster.Application.Exceptions;
using SnowflakeItemMaster.Domain.Providers;
using SnowflakeItemMaster.Provider.Snowflake.Constants;

namespace SnowflakeItemMaster.Provider.Snowflake.Repositories
{
    public class ItemPetalRepository : IItemPetalRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;

        public ItemPetalRepository(IDatabaseConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<List<ItemPetal>> GetItemsBySkusAsync(IEnumerable<string> skus)
        {
            var skuList = skus?.ToList() ?? new List<string>();
            if (!skuList.Any())
            {
                return new List<ItemPetal>();
            }

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var parameterNames = new List<string>();

            for (int i = 0; i < skuList.Count; i++)
            {
                var paramName = $":sku{i}";
                parameterNames.Add(paramName);
                parameters.Add($"sku{i}", skuList[i]);
            }

            var inClause = string.Join(",", parameterNames);
            var query = string.Format(SqlConstants.GetItemsBySkusQuery, inClause);

            var results = await connection.QueryAsync<ItemPetal>(query, parameters);

            return results.ToList();
        }

        public async Task<List<ItemPetal>> GetItemsWithLimitAsync(
            DateTime fromDate,
            DateTime toDate,
            int limit = 100)
        {
            if (limit <= 0)
                throw new ArgumentException("Limit must be greater than zero", nameof(limit));

            if (fromDate >= toDate)
                throw new ArgumentException("FromDate must be less than ToDate");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("fromDate", fromDate);
            parameters.Add("toDate", toDate);
            parameters.Add("limit", limit);

            var results = await connection.QueryAsync<ItemPetal>(
                SqlConstants.GetItemsWithLimitQuery,
                parameters);

            return results.ToList();
        }

        public async Task<List<ItemPetal>> GetItemsWithLimitAsync(int limit, int hours = 1)
        {
            var toDate = DateTime.UtcNow;
            var fromDate = toDate.AddHours(-hours);

            return await GetItemsWithLimitAsync(fromDate, toDate, limit);
        }
    }
}