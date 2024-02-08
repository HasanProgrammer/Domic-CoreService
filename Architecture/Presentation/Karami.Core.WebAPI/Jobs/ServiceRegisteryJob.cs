using Karami.Core.Common.ClassModels;
using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Host = Karami.Core.Common.ClassHelpers.Host;

namespace Karami.Core.WebAPI.Jobs;

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

        //Send event of self registration

        using var scope = _serviceScopeFactory.CreateScope();

        try
        {
            _messageBroker.Publish(new MessageBrokerDto<ServiceStatus> {
                Message = new ServiceStatus {
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
            e.FileLogger(_hostEnvironment, scope.ServiceProvider.GetRequiredService<IDateTime>());
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}