using System.Reflection;
using Domic.Core.Domain.Attributes;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class EventConsumerJob : IHostedService
{
    private readonly IExternalMessageBroker _externalMessageBroker;
    private readonly IConfiguration _configuration;

    public EventConsumerJob(IExternalMessageBroker externalMessageBroker, IConfiguration configuration)
    {
        _externalMessageBroker = externalMessageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalMessageBroker.NameOfAction  = nameof(EventConsumerJob);
        _externalMessageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
        var eventTypes =
            domainTypes.Where(type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false);

        var allQueues = eventTypes.Select(type =>
            (type.GetCustomAttribute(typeof(MessageBrokerAttribute)) as MessageBrokerAttribute)?.Queue
        );

        var allValidQueues = allQueues.Where(queue => !string.IsNullOrEmpty(queue)).Distinct();

        foreach (var queue in allValidQueues)
            if(_configuration.GetValue<bool>("IsExternalBrokerConsumingAsync"))
                _LongRunningListenerAsNonBlocking(queue, cancellationToken);
            else
                _LongRunningListenerAsNonBlocking(queue);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/

    private void _LongRunningListenerAsNonBlocking(string queue)
    {
        Task.Factory.StartNew(() => _externalMessageBroker.Subscribe(queue),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlocking(string queue, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalMessageBroker.SubscribeAsynchronously(queue, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}