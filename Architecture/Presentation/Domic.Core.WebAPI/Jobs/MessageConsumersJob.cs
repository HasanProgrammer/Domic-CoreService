#pragma warning disable CS8604

using System.Reflection;
using Domic.Core.UseCase.Commons.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class MessageConsumersJob : IHostedService
{
    private readonly IExternalMessageBroker _externalMessageBroker;
    private readonly IConfiguration _configuration;

    public MessageConsumersJob(IExternalMessageBroker externalMessageBroker, IConfiguration configuration)
    {
        _externalMessageBroker = externalMessageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalMessageBroker.NameOfAction  = nameof(MessageConsumersJob);
        _externalMessageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        var messageHandlerTypes = useCaseTypes.Where(type =>
            type.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerMessageBusHandler<>)
            )
        );

        var handlerInfo = messageHandlerTypes.Select(type => new {
                Queue = ( type.GetCustomAttribute(typeof(ConsumerAttribute)) as ConsumerAttribute )?.Queue,
                MessageType = type.GetInterfaces().FirstOrDefault()?.GetGenericArguments().First()
            }
        );

        foreach (var info in handlerInfo)
            if(info.Queue is not null)
                if(_configuration.GetValue<bool>("IsExternalBrokerConsumingAsync"))
                    _LongRunningListenerAsNonBlocking(info.Queue, info.MessageType, cancellationToken);
                else
                    _LongRunningListenerAsNonBlocking(info.Queue, info.MessageType);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    /*---------------------------------------------------------------*/

    private void _LongRunningListenerAsNonBlocking(string queue, Type messageType)
    {
        Task.Factory.StartNew(() => _externalMessageBroker.Subscribe(queue, messageType),
            TaskCreationOptions.LongRunning
        );
    }
    
    private void _LongRunningListenerAsNonBlocking(string queue, Type messageType, CancellationToken cancellationToken)
    {
        Task.Factory.StartNew(() => _externalMessageBroker.SubscribeAsynchronously(queue, messageType, cancellationToken),
            TaskCreationOptions.LongRunning
        );
    }
}