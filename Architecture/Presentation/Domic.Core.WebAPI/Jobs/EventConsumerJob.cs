using System.Reflection;
using Domic.Core.Domain.Attributes;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class EventConsumerJob : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly IConfiguration _configuration;

    public EventConsumerJob(IMessageBroker messageBroker, IConfiguration configuration)
    {
        _messageBroker = messageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messageBroker.NameOfAction  = nameof(EventConsumerJob);
        _messageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
        var eventTypes =
            domainTypes.Where(type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false);

        var allQueues = eventTypes.Select(type =>
            (type.GetCustomAttribute(typeof(MessageBrokerAttribute)) as MessageBrokerAttribute)?.Queue
        );

        var allValidQueues = allQueues.Where(queue => !string.IsNullOrEmpty(queue)).Distinct();

        foreach (var queue in allValidQueues)
            if(_configuration.GetValue<bool>("IsBrokerConsumingAsync"))
                _LongRunningListenerAsNonBlocking(queue, cancellationToken);
            else
                _LongRunningListenerAsNonBlocking(queue);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/

    private void _LongRunningListenerAsNonBlocking(string queue)
    {
        Task.Factory.StartNew(() => _messageBroker.Subscribe(queue),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlocking(string queue, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _messageBroker.SubscribeAsynchronously(queue, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}