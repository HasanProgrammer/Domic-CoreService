using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.Common.ClassModels;

namespace Domic.Core.UseCase.Caches;

public class FetchServicesInfoInMemoryCache : IMemoryCacheSetter<List<ServiceStatus>>
{
    private readonly IServiceDiscovery _serviceDiscovery;

    public FetchServicesInfoInMemoryCache(IServiceDiscovery serviceDiscovery) => _serviceDiscovery = serviceDiscovery;

    [Config(Key = "ServicesInfo", Ttl = 1)]
    public Task<List<ServiceStatus>> SetAsync(CancellationToken cancellationToken)
        => _serviceDiscovery.FetchAllServicesInfoAsync(cancellationToken);
}