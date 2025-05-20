using Microsoft.Extensions.DependencyInjection;
using SnowflakeItemMaster.Application.Interfaces;
using SnowflakeItemMaster.Application.UseCases;

namespace SnowflakeItemMaster.Application.Setup
{
    public static class IocModule
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IConfigWrapper, ConfigWrapper>();
            serviceCollection.AddScoped<ISkuProcessor, SkuProcessor>();
            serviceCollection.AddScoped<ITransformService, TransformService>();
            return serviceCollection;
        }
    }
}