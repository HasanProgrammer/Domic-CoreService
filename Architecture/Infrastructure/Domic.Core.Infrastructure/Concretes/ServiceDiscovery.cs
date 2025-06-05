#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Domic.Core.Common.ClassModels;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.Service.Grpc;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using String = Domic.Core.Service.Grpc.String;

namespace Domic.Core.Infrastructure.Concretes;

public sealed class ServiceDiscovery : IServiceDiscovery
{
    private readonly IHostEnvironment                        _hostEnvironment;
    private readonly IExternalDistributedCacheMediator       _externalDistributedCacheMediator;
    private readonly IDateTime                               _dateTime;
    private readonly IGlobalUniqueIdGenerator                _globalUniqueIdGenerator;
    private readonly IConfiguration                          _configuration;
    private readonly IServiceProvider                        _serviceProvider;
    private readonly DiscoveryService.DiscoveryServiceClient _discoveryServiceClient;

    public ServiceDiscovery(DiscoveryService.DiscoveryServiceClient discoveryServiceClient, 
        IHostEnvironment hostEnvironment, IExternalDistributedCacheMediator externalDistributedCacheMediator,
        IDateTime dateTime, IGlobalUniqueIdGenerator globalUniqueIdGenerator, IConfiguration configuration,
        IServiceProvider serviceProvider
    )
    {
        _hostEnvironment                  = hostEnvironment;
        _discoveryServiceClient           = discoveryServiceClient;
        _externalDistributedCacheMediator = externalDistributedCacheMediator;
        _dateTime                         = dateTime;
        _globalUniqueIdGenerator          = globalUniqueIdGenerator;
        _configuration                    = configuration;
        _serviceProvider                  = serviceProvider;
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
            var targetInstanceOfServiceIndex = Random.Shared.Next(currentServiceInstancesInfo.Count);

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
            return await Policy.Handle<Exception>()
                               .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3))
                               .Execute(() =>
                                   _externalDistributedCacheMediator.GetAsync<List<ServiceStatus>>(cancellationToken)
                               );
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                _configuration.GetValue<string>("NameOfService"), "LoadServicesInfoAsync"
            );

            if (_configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
            {
                var messageBroker = _serviceProvider.GetRequiredService<IExternalMessageBroker>();

                //fire&forget
                e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, messageBroker, _dateTime,
                    _configuration.GetValue<string>("NameOfService"), "LoadServicesInfoAsync", cancellationToken
                );
            }
            else
            {
                var streamBroker = _serviceProvider.GetRequiredService<IExternalEventStreamBroker>();

                //fire&forget
                e.CentralExceptionLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, streamBroker,
                    _dateTime, _configuration.GetValue<string>("NameOfService"), "LoadServicesInfoAsync", 
                    cancellationToken
                );
            }
        }

        return await FetchAllServicesInfoAsync(cancellationToken);
    }
}