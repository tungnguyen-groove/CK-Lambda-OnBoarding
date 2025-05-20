using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SnowflakeItemMaster.Application.Setup;
using SnowflakeItemMaster.Infrastructure;
using SnowflakeItemMaster.Infrastructure.Setup;
using SnowflakeItemMaster.Provider.Snowflake.Setup;
using SnowflakeItemMaster.Application.Contracts.Logger;

namespace SnowflakeItemMaster.Lambda;

public abstract class FunctionBase
{
    protected readonly IServiceCollection _serviceCollection;
    protected readonly ServiceProvider _serviceProvider;
    protected readonly ILoggingService _logger;

    protected FunctionBase()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        _serviceCollection = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddApplicationServices()
            .AddProviderSnowflakeServices(configuration)
            .AddInfrastructureServices(configuration);

        _serviceProvider = _serviceCollection.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILoggingService>();
    }

    protected T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }
} 