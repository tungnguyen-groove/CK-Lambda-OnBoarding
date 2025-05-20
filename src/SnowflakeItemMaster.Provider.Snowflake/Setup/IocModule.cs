using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnowflakeItemMaster.Application.Contracts.DatabaseConnection;
using SnowflakeItemMaster.Application.Contracts.Provider;
using SnowflakeItemMaster.Application.Settings;
using SnowflakeItemMaster.Provider.Snowflake.Repositories;

namespace SnowflakeItemMaster.Provider.Snowflake.Setup
{
    public static class IocModule
    {
        public static IServiceCollection AddProviderSnowflakeServices(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            // Register database connection factory
            serviceCollection.AddSingleton<IDatabaseConnectionFactory, SnowflakeConnectionFactory>();

            // Register repository
            serviceCollection.AddScoped<IItemPetalRepository, ItemPetalRepository>();

            return serviceCollection;
        }
    }
}