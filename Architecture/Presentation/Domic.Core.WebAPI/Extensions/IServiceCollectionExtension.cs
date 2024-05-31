using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Extensions;

public static class IServiceCollectionExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="hostedServiceType"></param>
    /// <returns></returns>
    public static IServiceCollection AddHostedService(this IServiceCollection services, Type hostedServiceType)
    {
        if (hostedServiceType.GetInterfaces().All(i => i != typeof(IHostedService)))
            throw new Exception("[ HostedServiceType ] must be inherited from IHostedService !");
        
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceType));

        return services;
    }
}