using Karami.Core.Grpc.Service;
using Karami.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.Infrastructure.Implementations;

using String = Core.Grpc.Service.String;

public class ServiceDiscovery : IServiceDiscovery
{
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DiscoveryService.DiscoveryServiceClient _discoveryServiceClient;

    public ServiceDiscovery(DiscoveryService.DiscoveryServiceClient discoveryServiceClient, 
        IHostEnvironment hostEnvironment
    )
    {
        _hostEnvironment        = hostEnvironment;
        _discoveryServiceClient = discoveryServiceClient;
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
        var request = new ReadAllRequest {
            Name = new String { Value = serviceName }
        };

        var result = await _discoveryServiceClient.ReadAllAsync(request, cancellationToken: cancellationToken);

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