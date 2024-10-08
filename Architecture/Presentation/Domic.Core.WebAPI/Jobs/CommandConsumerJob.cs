﻿using System.Reflection;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class CommandConsumerJob : IHostedService
{
    private readonly IConfiguration      _configuration;
    private readonly IInternalMessageBroker _commandBroker;

    public CommandConsumerJob(IInternalMessageBroker commandBroker, IConfiguration configuration)
    {
        _configuration = configuration;
        _commandBroker = commandBroker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _commandBroker.NameOfAction  = nameof(CommandConsumerJob);
        _commandBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        var asyncCommandTypes =
            useCaseTypes.Where(type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IAsyncCommand)) ?? false);

        var allQueues = asyncCommandTypes.Select(type =>
            (type.GetCustomAttribute(typeof(QueueableAttribute)) as QueueableAttribute)?.Queue
        );

        var allValidQueues = allQueues.Where(queue => !string.IsNullOrEmpty(queue)).Distinct();

        foreach (var queue in allValidQueues)
            if(_configuration.GetValue<bool>("IsInternalBrokerConsumingAsync"))
                _LongRunningListenerAsNonBlocking(queue, cancellationToken);
            else
                _LongRunningListenerAsNonBlocking(queue);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/

    private void _LongRunningListenerAsNonBlocking(string queue)
    {
        Task.Factory.StartNew(() => _commandBroker.Subscribe(queue),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlocking(string queue, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _commandBroker.SubscribeAsynchronously(queue, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}