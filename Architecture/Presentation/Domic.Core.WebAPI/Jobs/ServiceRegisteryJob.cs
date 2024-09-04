#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Domic.Core.Common.ClassModels;
using Domic.Core.Domain.Constants;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Host = Domic.Core.Common.ClassHelpers.Host;

namespace Domic.Core.WebAPI.Jobs;

public class ServiceRegisteryJob : IHostedService
{
    private readonly IMessageBroker       _messageBroker;
    private readonly IConfiguration       _configuration;
    private readonly IHostEnvironment     _hostEnvironment;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ServiceRegisteryJob(IConfiguration configuration, IHostEnvironment hostEnvironment, 
        IMessageBroker messageBroker, IServiceScopeFactory serviceScopeFactory
    )
    {
        _configuration       = configuration;
        _messageBroker       = messageBroker;
        _hostEnvironment     = hostEnvironment;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = _configuration.GetValue<string>("NameOfService");
        var serviceHost = Environment.GetEnvironmentVariable("Host");
        var servicePort = Environment.GetEnvironmentVariable("Port");

        //send event of self registration

        using var scope = _serviceScopeFactory.CreateScope();

        var globalUniqueIdGenerator = scope.ServiceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();

        try
        {
            _messageBroker.Publish(new MessageBrokerDto<ServiceStatus> {
                Message = new ServiceStatus {
                    Id        = globalUniqueIdGenerator.GetRandom(6),
                    Name      = serviceName         ,
                    Host      = serviceHost         ,
                    IPAddress = Host.GetIPAddress() ,
                    Port      = servicePort         ,
                    Status    = true 
                },
                ExchangeType = Exchange.Direct                 ,
                Exchange     = Broker.ServiceRegistry_Exchange ,
                Route        = Broker.ServiceRegistry_Route    ,
                Queue        = Broker.ServiceRegistry_Queue 
            });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, scope.ServiceProvider.GetRequiredService<IDateTime>(), 
                cancellationToken: cancellationToken
            );
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}