using System.Reflection;
using System.Text;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Attributes;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Attributes;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Karami.Core.UseCase.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Karami.Core.Domain.Implementations;

public class MessageBroker : IMessageBroker
{
    private static object _lock = new();
    
    private readonly IConnection          _connection;
    private readonly IHostEnvironment     _hostEnvironment;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDotrisDateTime      _dotrisDateTime;

    public MessageBroker(
        IConfiguration       configuration       ,
        IHostEnvironment     hostEnvironment     ,
        IServiceScopeFactory serviceScopeFactory ,
        IDotrisDateTime      dotrisDateTime
    )
    {
        _hostEnvironment     = hostEnvironment;
        _serviceScopeFactory = serviceScopeFactory;
        _dotrisDateTime      = dotrisDateTime;
        
        var factory = new ConnectionFactory {
            HostName = configuration.GetExternalRabbitHostName(),
            UserName = configuration.GetExternalRabbitUsername(),
            Password = configuration.GetExternalRabbitPassword(),
            Port     = configuration.GetExternalRabbitPort() 
        };
        
        _connection = factory.CreateConnection();
    }

    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    public void Publish<TMessage>(MessageBrokerDto<TMessage> messageBroker) where TMessage : class
    {
        using var channel = _connection.CreateModel();

        switch (messageBroker.ExchangeType)
        {
            case Exchange.Direct :
                channel.PublishMessageToDirectExchange(
                    messageBroker.Message.Serialize(), messageBroker.Exchange, messageBroker.Route, 
                    messageBroker.Headers
                );
            break;

            case Exchange.FanOut :
                channel.PublishMessageToFanOutExchange(
                    messageBroker.Message.Serialize(), messageBroker.Exchange
                );
            break;

            case Exchange.Unknown :
                channel.PublishMessage(messageBroker.Message.Serialize(), messageBroker.Queue);
            break;

            default: throw new ArgumentOutOfRangeException();
        }
    }

    public void Publish()
    {
        lock (_lock)
        {
            //ScopeServices trigger
            using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();

            var commandUnitOfWork =
                serviceScope.ServiceProvider.GetRequiredService(_getTypeOfCommandUnitOfWork()) as ICoreCommandUnitOfWork;

            var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();

            var channel = _connection.CreateModel();

            try
            {
                commandUnitOfWork.Transaction();

                foreach (Event targetEvent in eventCommandRepository.FindAllWithOrdering(Order.Date))
                {
                    if (targetEvent.IsActive == IsActive.Active)
                    {
                        _eventPublishHandler(channel, targetEvent);

                        var nowDateTime        = DateTime.Now;
                        var nowPersianDateTime = _dotrisDateTime.ToPersianShortDate(nowDateTime);

                        targetEvent.IsActive              = IsActive.InActive;
                        targetEvent.UpdatedAt_EnglishDate = nowDateTime;
                        targetEvent.UpdatedAt_PersianDate = nowPersianDateTime;

                        eventCommandRepository.Change(targetEvent);
                    }
                    else
                        eventCommandRepository.Remove(targetEvent);
                }

                commandUnitOfWork.Commit();
            }
            catch (Exception e)
            {
                e.FileLogger(_hostEnvironment, _dotrisDateTime);
                e.CentralExceptionLogger(_hostEnvironment, this, _dotrisDateTime, NameOfService, NameOfAction);

                commandUnitOfWork?.Rollback();
            }
            finally
            {
                channel.Dispose();
            }
        }
    }

    public void Subscribe<TMessage>(string queue) where TMessage : class
    {
        try
        {
            var channel = _connection.CreateModel();
            
            EventingBasicConsumer consumer = new(channel);

            consumer.Received += (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<TMessage>();

                _messageOfQueueHandle(channel, args, message, serviceScope.ServiceProvider);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
    }

    public void Subscribe(string queue)
    {
        try
        {
            var channel = _connection.CreateModel();
            
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var @event = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<Event>();
                
                _eventOfQueueHandle(channel, args, @event, NameOfService, serviceScope.ServiceProvider);
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
    
    /*---------------------------------------------------------------*/

    private void _eventPublishHandler(IModel channel, Event @event)
    {
        var nameOfEvent = @event.Type;

        var domainTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();
        
        var typeOfEvents = domainTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false
        );

        var typeOfEvent = typeOfEvents.FirstOrDefault(type => type.Name.Equals(nameOfEvent));

        var messageBroker = typeOfEvent.GetCustomAttribute(typeof(MessageBrokerAttribute)) as MessageBrokerAttribute;

        switch (messageBroker.ExchangeType)
        {
            case Exchange.Direct :
                channel.PublishMessageToDirectExchange(
                    @event.Serialize(), messageBroker.Exchange, messageBroker.Route
                );
            break;
            
            case Exchange.FanOut :
                channel.PublishMessageToFanOutExchange(
                    @event.Serialize(), messageBroker.Exchange
                );
            break;

            default : throw new ArgumentOutOfRangeException();
        }
    }

    private void _messageOfQueueHandle<TMessage>(IModel channel, BasicDeliverEventArgs args, TMessage message,
        IServiceProvider serviceProvider
    ) where TMessage : class
    {
        ICoreUnitOfWork unitOfWork = null;

        try
        {
            var messageBusHandler     = serviceProvider.GetRequiredService(typeof(IConsumerMessageBusHandler<TMessage>));
            var messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            if (_isMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        messageBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                    
                    afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new object[] { message });
                }
                
                _trySendAckMessage(channel, args); //Remove message
            }
            else
            {
                if (messageBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_getTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message });

                    unitOfWork.Commit();
                }
                else
                    messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message });

                _cleanCache(messageBusHandlerMethod, serviceProvider);
            
                _trySendAckMessage(channel, args); //Consume Message Of Queue & Delete This Message From Queue
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
            
            unitOfWork?.Rollback();

            _requeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _eventOfQueueHandle(IModel channel, BasicDeliverEventArgs args, Event @event, string service,
        IServiceProvider serviceProvider
    )
    {
        Type eventBusHandlerType   = null;
        ICoreUnitOfWork unitOfWork = null;

        try
        {
            var useCaseTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();

            var targetConsumerEventBusHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerEventBusHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(@event.Type))
                )
            );

            if (targetConsumerEventBusHandlerType is not null)
            {
                var eventType = targetConsumerEventBusHandlerType.GetInterfaces()
                                                                 .Select(i => i.GetGenericArguments()[0])
                                                                 .FirstOrDefault();
        
                var fullContractOfConsumerType = typeof(IConsumerEventBusHandler<>).MakeGenericType(eventType);
            
                var eventBusHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);
                eventBusHandlerType = eventBusHandler.GetType();
                
                var payload = JsonConvert.DeserializeObject(@event.Payload, eventType);

                var eventBusHandlerMethod =
                    eventBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");

                var retryAttr =
                    eventBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

                if (_isMaxRetryMessage(args, retryAttr))
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            eventBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                        
                        afterMaxRetryHandlerMethod.Invoke(eventBusHandler, new object[] { payload });
                    }
                    
                    _trySendAckMessage(channel, args); //Remove message
                }
                else
                {
                    if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_getTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                        unitOfWork.Transaction(transactionAttr.IsolationLevel);

                        eventBusHandlerMethod.Invoke(eventBusHandler, new object[] { payload });

                        unitOfWork.Commit();
                    }
                    else
                        eventBusHandlerMethod.Invoke(eventBusHandler, new object[] { payload });

                    _cleanCache(eventBusHandlerMethod, serviceProvider);
                    
                    _trySendAckMessage(channel, args); //Consume Message Of Queue & Delete This Message From Queue
                }
            }
            else
                _trySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
            
            e.CentralExceptionLogger(_hostEnvironment, this, _dotrisDateTime, service, 
                eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );

            unitOfWork?.Rollback();

            _requeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _requeueMessageAsDeadLetter(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            channel.BasicNack(args.DeliveryTag, false, false); //or _channel.BasicReject(args.DeliveryTag, false);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
    }
    
    private bool _isMaxRetryMessage(BasicDeliverEventArgs args, WithMaxRetryAttribute maxRetryAttribute)
    {
        var xDeath = args.BasicProperties.Headers?.FirstOrDefault(header => header.Key.Equals("x-death")).Value;

        var xDeathInfo = (xDeath as List<object>)?.FirstOrDefault() as Dictionary<string, object>;
                
        var countRetry = xDeathInfo?.FirstOrDefault(header => header.Key.Equals("count")).Value;

        return Convert.ToInt32(countRetry) > maxRetryAttribute?.Count;
    }
    
    private void _trySendAckMessage(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            channel.BasicAck(args.DeliveryTag, false); //Delete This Message From Queue
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
    }
    
    private Type _getTypeOfUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork))
        ) ?? domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private Type _getTypeOfCommandUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private void _cleanCache(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider)
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
            {
                var redisCache = serviceProvider.GetRequiredService<IRedisCache>();

                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    redisCache.DeleteKey(key);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
    }
}