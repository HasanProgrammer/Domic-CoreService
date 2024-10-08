﻿#pragma warning disable CS8604

using System.Reflection;
using Domic.Core.UseCase.Commons.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class MessageStreamConsumerJob : IHostedService
{
    private readonly IExternalEventStreamBroker _externalEventStreamBroker;
    private readonly IConfiguration _configuration;

    public MessageStreamConsumerJob(IExternalEventStreamBroker externalEventStreamBroker, IConfiguration configuration)
    {
        _externalEventStreamBroker = externalEventStreamBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalEventStreamBroker.NameOfAction  = nameof(MessageStreamConsumerJob);
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
            if(topic is not null)
                if (_configuration.GetValue<bool>("IsExternalBrokerConsumingAsync"))
                    _LongRunningListenerAsNonBlockingAndAsynchronously(topic, cancellationToken);
                else
                    _LongRunningListenerAsNonBlockingAndSequential(topic, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/
    
    private void _LongRunningListenerAsNonBlockingAndSequential(string topic, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeMessage(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlockingAndAsynchronously(string topic, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalEventStreamBroker.SubscribeMessageAsynchronously(topic, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}