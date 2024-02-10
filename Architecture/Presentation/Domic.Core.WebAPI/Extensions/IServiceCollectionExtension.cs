//Scope  | PerRequest , One Object   | به ازای هر درخواست ؛ تنها یک شی از شی مورد نظر میسازد
//Trans  | PerRequest , Multi Object | به ازای هر درخواست ؛ هر بار که شی مورد نظر خواسته شود ، آن را میسازد
//Single | AllRequest , One Object   | برای هر بار درخواست تا موقعی که شی ساخته شده در حافظه باشد ؛ از همان استفاده می کند

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