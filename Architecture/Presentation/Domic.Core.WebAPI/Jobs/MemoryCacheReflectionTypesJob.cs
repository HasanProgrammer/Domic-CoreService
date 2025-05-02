using System.Reflection;
using Domic.Core.Domain.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class MemoryCacheReflectionTypesJob(IServiceScopeFactory serviceScopeFactory) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var serializer  = scope.ServiceProvider.GetRequiredService<ISerializer>();
        var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        memoryCache.Set("DomainTypes" , serializer.Serialize(Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes()));
        memoryCache.Set("UseCaseTypes", serializer.Serialize(Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes()));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}