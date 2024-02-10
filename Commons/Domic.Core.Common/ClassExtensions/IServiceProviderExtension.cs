using Microsoft.Extensions.DependencyInjection;

namespace Domic.Core.Common.ClassExtensions;

public static class IServiceProviderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetScopeService<T>(this IServiceProvider serviceProvider)
    {
        using IServiceScope Scope = serviceProvider.CreateScope();
        return Scope.ServiceProvider.GetService<T>();
    }
}