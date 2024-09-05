#pragma warning disable CS8604

using System.Reflection;
using Domic.Core.UseCase.Commons.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class MessageRetriableStreamConsumerJob : IHostedService
{
    private readonly IExternalEventStreamBroker _externalEventStreamBroker;
    private readonly IConfiguration _configuration;

    public MessageRetriableStreamConsumerJob(IExternalEventStreamBroker externalEventStreamBroker, IConfiguration configuration)
    {
        _externalEventStreamBroker = externalEventStreamBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalEventStreamBroker.NameOfAction  = nameof(MessageRetriableStreamConsumerJob);
        _externalEventStreamBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        var messageStreamHandlerTypes = useCaseTypes.Where(type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>)
            )
        );

        var topics = messageStreamHandlerTypes.Select(type => 
            ( type.GetCustomAttribute(typeof(StreamConsumerAttribute)) as StreamConsumerAttribute )?.Topic
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
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeRetriableMessage(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlockingAndAsynchronously(string topic, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeRetriableMessageAsynchronously(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}