using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.Common.ClassModels;

namespace Domic.Core.UseCase.Caches;

public class FetchServicesInfoExternalDistributedCacheHandler : IExternalDistributedCacheHandler<List<ServiceStatus>>
{
    private readonly IServiceDiscovery _serviceDiscovery;

    public FetchServicesInfoExternalDistributedCacheHandler(IServiceDiscovery serviceDiscovery) 
        => _serviceDiscovery = serviceDiscovery;

    [Config(Key = "ServicesInfo")]
    public Task<List<ServiceStatus>> SetAsync(CancellationToken cancellationToken)
        => _serviceDiscovery.FetchAllServicesInfoAsync(cancellationToken);
}