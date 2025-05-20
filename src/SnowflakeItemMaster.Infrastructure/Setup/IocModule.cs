using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SnowflakeItemMaster.Application.Contracts.Logger;
using SnowflakeItemMaster.Application.Contracts.MessageQueue;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Infrastructure.Repositories;
using SnowflakeItemMaster.Infrastructure.Services;

namespace SnowflakeItemMaster.Infrastructure.Setup
{
    public static class IocModule
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register AWS options
            services.AddDefaultAWSOptions(new AWSOptions
            {
                Region = RegionEndpoint.GetBySystemName(
                     services.BuildServiceProvider()
                    .GetRequiredService<IConfigWrapper>()
                    .GetAwsSqsSettings()
                    .Region
                )
            });

            // Register AWS SQS Client
            services.AddAWSService<IAmazonSQS>();

            // Register repositories
            services.AddScoped<IRepositoryManager, RepositoryManager>();

            // Register logging service
            services.AddSingleton<ILoggingService, CloudWatchLoggingService>();

            // Register services
            services.AddScoped<IMessageQueueService, AwsSqsMessageQueueService>();

            // Register database settings from configuration
            services.AddDbContext<RepositoryContext>((sp, options) =>
            {
                var configWrapper = sp.GetRequiredService<IConfigWrapper>();
                var databaseSettings = configWrapper.GetDatabaseSettings();
                options.UseMySql(
                    databaseSettings.ConnectionString,
                    ServerVersion.AutoDetect(databaseSettings.ConnectionString)
                );
            });

            return services;
        }
    }
}