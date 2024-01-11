using Karami.Core.Common.ClassModels;
using Karami.Core.Grpc.Service;
using Karami.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.Infrastructure.Implementations;

using String = Core.Grpc.Service.String;

public class ServiceDiscovery : IServiceDiscovery
{
    private readonly IHostEnvironment                        _hostEnvironment;
    private readonly ICacheService                           _cacheService;
    private readonly DiscoveryService.DiscoveryServiceClient _discoveryServiceClient;

    public ServiceDiscovery(DiscoveryService.DiscoveryServiceClient discoveryServiceClient, 
        IHostEnvironment hostEnvironment, ICacheService cacheService
    )
    {
        _hostEnvironment        = hostEnvironment;
        _cacheService           = cacheService;
        _discoveryServiceClient = discoveryServiceClient;
    }

    public async Task<List<ServiceStatus>> FetchAllServicesInfoAsync(CancellationToken cancellationToken)
    {
        var request = new ReadAllRequest();

        var result = await _discoveryServiceClient.ReadAllAsync(request, cancellationToken: cancellationToken);

        if (result?.Code != 200)
            return default;
        
        return result.Body.Services.Select(service =>
                   new ServiceStatus {
                       Name         = service.Name.Value,
                       Host         = service.Host.Value,
                       IPAddress    = service.IpAddress.Value,
                       Port         = service.Port.Value.ToString(),
                       Status       = service.Status,
                       ResponseTime = service.ResponseTime.Value
                   }
               )
               .ToList();
    }

    public async Task<string> LoadAddressInMemoryAsync(string serviceName, CancellationToken cancellationToken)
    {
        var servicesInfo = await _cacheService.GetAsync<List<ServiceStatus>>(cancellationToken);

        var currentServiceInstancesInfo = servicesInfo.Where(service => service.Name.Equals(serviceName)).ToList();

        #region LoadBalance

        //ToDo : ( Tech Debt ) -> Should be used thread safty way for [Random]
        
        var random = new Random();

        var targetInstanceOfServiceIndex = random.Next(currentServiceInstancesInfo.Count);

        var targetInstanceOfService = currentServiceInstancesInfo[targetInstanceOfServiceIndex];

        #endregion
        
        var endpoint =
            _hostEnvironment.IsProduction() ? targetInstanceOfService.IPAddress : targetInstanceOfService.Host;

        return $"https://{endpoint}:{targetInstanceOfService.Port}";
    }

    public async Task<string> LoadAddressAsync(string serviceName, CancellationToken cancellationToken)
    {
        var request = new ReadOneRequest {
            Name = new String { Value = serviceName }
        };

        var result = await _discoveryServiceClient.ReadOneAsync(request, cancellationToken: cancellationToken);

        if (result?.Code == 200)
        {
            var endpoint =
                _hostEnvironment.IsProduction() ? result.Body.Service.IpAddress.Value : result.Body.Service.Host.Value;

            return $"https://{endpoint}:{result.Body.Service.Port.Value}";
        }

        return default;
    }

    public async Task<List<string>> LoadAddressesAsync(string serviceName, CancellationToken cancellationToken)
    {
        var request = new ReadAllByNameRequest {
            Name = new String { Value = serviceName }
        };

        var result = await _discoveryServiceClient.ReadAllByNameAsync(request, cancellationToken: cancellationToken);

        if (result?.Code == 200)
        {
            string Endpoint(Service service)
                => _hostEnvironment.IsProduction() ? service.IpAddress.Value : service.Host.Value;
            
            return result.Body.Services.Select(service => 
                $"https://{Endpoint(service)}:{service.Port.Value}"
            ).ToList();
        }

        return default;
    }

    public void Dispose(){}
}