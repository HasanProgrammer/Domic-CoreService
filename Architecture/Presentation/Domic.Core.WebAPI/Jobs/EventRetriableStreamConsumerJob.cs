#pragma warning disable CS8604

using System.Reflection;
using Domic.Core.Domain.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class EventRetriableStreamConsumerJob : IHostedService
{
    private readonly IExternalEventStreamBroker _externalEventStreamBroker;
    private readonly IConfiguration _configuration;

    public EventRetriableStreamConsumerJob(IExternalEventStreamBroker externalEventStreamBroker, IConfiguration configuration)
    {
        _externalEventStreamBroker = externalEventStreamBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalEventStreamBroker.NameOfAction  = nameof(EventRetriableStreamConsumerJob);
        _externalEventStreamBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        var eventStreamHandlerTypes = useCaseTypes.Where(type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>)
            )
        );

        var topics = eventStreamHandlerTypes.Select(type => 
            ( type.GetCustomAttribute(typeof(EventConfigAttribute)) as EventConfigAttribute )?.Topic
        );

        foreach (var topic in topics)
            if (topic is not null)
            {
                var retryTopic = $"{_externalEventStreamBroker.NameOfService}-Retry-{topic}";
                
                if (_configuration.GetValue<bool>("IsExternalBrokerConsumingAsync"))
                    _LongRunningListenerAsNonBlockingAndAsynchronously(retryTopic, cancellationToken);
                else
                    _LongRunningListenerAsNonBlockingAndSynchronously(retryTopic, cancellationToken);
            }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/
    
    private void _LongRunningListenerAsNonBlockingAndSynchronously(string topic, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeRetriable(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlockingAndAsynchronously(string topic, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeRetriableAsynchronously(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}