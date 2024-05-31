using Domic.Core.Common.ClassModels;
using Domic.Core.Service.Grpc;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Hosting;

using String = Domic.Core.Service.Grpc.String;

namespace Domic.Core.Infrastructure.Concretes;

public class ServiceDiscovery : IServiceDiscovery
{
    private readonly IHostEnvironment                        _hostEnvironment;
    private readonly IExternalDistributedCacheMediator       _externalDistributedCacheMediator;
    private readonly DiscoveryService.DiscoveryServiceClient _discoveryServiceClient;

    public ServiceDiscovery(DiscoveryService.DiscoveryServiceClient discoveryServiceClient, 
        IHostEnvironment hostEnvironment, IExternalDistributedCacheMediator externalDistributedCacheMediator
    )
    {
        _hostEnvironment                  = hostEnvironment;
        _discoveryServiceClient           = discoveryServiceClient;
        _externalDistributedCacheMediator = externalDistributedCacheMediator;
    }

    public async Task<List<ServiceStatus>> FetchAllServicesInfoAsync(CancellationToken cancellationToken)
    {
        var request = new ReadAllRequest();

        //all of instances that status = true
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
        var servicesInfo = await _LoadServicesInfoAsync(cancellationToken);

        var currentServiceInstancesInfo = servicesInfo.Where(service => service.Name.Equals(serviceName)).ToList();

        #region LoadBalance

        var targetInstance = currentServiceInstancesInfo.MinBy(status => status.ResponseTime);

        if (targetInstance is null)
        {
            var random = new Random(); //ToDo : ( Tech Debt ) -> Should be used thread safty way for [Random]

            var targetInstanceOfServiceIndex = random.Next(currentServiceInstancesInfo.Count);

            targetInstance = currentServiceInstancesInfo[targetInstanceOfServiceIndex];
        }

        #endregion
        
        var endpoint =
            _hostEnvironment.IsProduction() ? targetInstance.IPAddress : targetInstance.Host;

        return $"https://{endpoint}:{targetInstance.Port}";
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
            string Endpoint(Service.Grpc.Service service)
                => _hostEnvironment.IsProduction() ? service.IpAddress.Value : service.Host.Value;
            
            return result.Body.Services.Select(service => 
                $"https://{Endpoint(service)}:{service.Port.Value}"
            ).ToList();
        }

        return default;
    }

    public void Dispose(){}
    
    /*---------------------------------------------------------------*/

    private async Task<List<ServiceStatus>> _LoadServicesInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            var servicesInfo = await _externalDistributedCacheMediator.GetAsync<List<ServiceStatus>>(cancellationToken);

            return servicesInfo;
        }
        catch (Exception e)
        {
            //ToDo : ( Tech Debt ) => Should be used log in here!
        }

        return await FetchAllServicesInfoAsync(cancellationToken);
    }
}