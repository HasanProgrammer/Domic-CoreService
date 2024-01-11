using Karami.Core.Common.ClassModels;
using Karami.Core.UseCase.Attributes;
using Karami.Core.UseCase.Contracts.Interfaces;

namespace Karami.Core.UseCase.Caches;

public class FetchServicesInfoInMemoryCache : IMemoryCacheSetter<List<ServiceStatus>>
{
    private readonly IServiceDiscovery _serviceDiscovery;

    public FetchServicesInfoInMemoryCache(IServiceDiscovery serviceDiscovery) => _serviceDiscovery = serviceDiscovery;

    [Config(Key = "ServicesInfo")]
    public Task<List<ServiceStatus>> SetAsync(CancellationToken cancellationToken)
        => _serviceDiscovery.FetchAllServicesInfoAsync(cancellationToken);
}