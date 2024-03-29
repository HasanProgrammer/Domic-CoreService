﻿using System.Reflection;
using System.Text;
using Domic.Core.Domain.Attributes;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Domic.Core.Infrastructure.Implementations;

public class MessageBroker : IMessageBroker
{
    private static object _lock = new();
    
    private readonly IConnection              _connection;
    private readonly IHostEnvironment         _hostEnvironment;
    private readonly IServiceScopeFactory     _serviceScopeFactory;
    private readonly IDateTime                _dateTime;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public MessageBroker(IConfiguration configuration, IHostEnvironment hostEnvironment, 
        IServiceScopeFactory serviceScopeFactory, IDateTime dateTime, IGlobalUniqueIdGenerator globalUniqueIdGenerator
    )
    {
        _hostEnvironment         = hostEnvironment;
        _serviceScopeFactory     = serviceScopeFactory;
        _dateTime                = dateTime;
        _globalUniqueIdGenerator = globalUniqueIdGenerator;
        
        var factory = new ConnectionFactory {
            HostName = configuration.GetExternalRabbitHostName(),
            UserName = configuration.GetExternalRabbitUsername(),
            Password = configuration.GetExternalRabbitPassword(),
            Port     = configuration.GetExternalRabbitPort() 
        };

        factory.DispatchConsumersAsync = configuration.GetValue<bool>("IsExternalBrokerConsumingAsync");
        
        _connection = factory.CreateConnection();
    }

    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    #region MessageStructure

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

                _MessageOfQueueHandle(channel, args, message, serviceScope.ServiceProvider);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void Subscribe(string queue, Type messageType)
    {
        try
        {
            var channel = _connection.CreateModel();
            
            EventingBasicConsumer consumer = new(channel);

            consumer.Received += (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize(messageType);

                _MessageOfQueueHandle(channel, args, message, serviceScope.ServiceProvider);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void SubscribeAsynchronously<TMessage>(string queue, CancellationToken cancellationToken) where TMessage : class
    {
        try
        {
            var channel = _connection.CreateModel();
            
            AsyncEventingBasicConsumer consumer = new(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<TMessage>();

                await _MessageOfQueueHandleAsync(channel, args, message, serviceScope.ServiceProvider, cancellationToken);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void SubscribeAsynchronously(string queue, Type messageType, CancellationToken cancellationToken)
    {
        try
        {
            var channel = _connection.CreateModel();
            
            AsyncEventingBasicConsumer consumer = new(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize(messageType);

                await _MessageOfQueueHandleAsync(channel, args, message, serviceScope.ServiceProvider, cancellationToken);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    #endregion

    #region EventStructure
    
    public void Publish()
    {
        lock (_lock)
        {
            //ScopeServices trigger
            using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();

            var commandUnitOfWork =
                serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfCommandUnitOfWork()) as ICoreCommandUnitOfWork;

            var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();

            var channel = _connection.CreateModel();

            try
            {
                commandUnitOfWork.Transaction();

                foreach (Event targetEvent in eventCommandRepository.FindAllWithOrdering(Order.Date))
                {
                    if (targetEvent.IsActive == IsActive.Active)
                    {
                        _EventPublishHandler(channel, targetEvent);

                        var nowDateTime        = DateTime.Now;
                        var nowPersianDateTime = _dateTime.ToPersianShortDate(nowDateTime);

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
                e.FileLogger(_hostEnvironment, _dateTime);
                
                e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                    NameOfService, NameOfAction
                );
                
                e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService, 
                    NameOfAction
                );

                commandUnitOfWork?.Rollback();
            }
            finally
            {
                channel.Dispose();
            }
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
                
                _EventOfQueueHandle(channel, args, @event, NameOfService, serviceScope.ServiceProvider);
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void SubscribeAsynchronously(string queue, CancellationToken cancellationToken)
    {
        try
        {
            var channel = _connection.CreateModel();
            
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var @event = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<Event>();
                
               await _EventOfQueueHandleAsync(channel, args, @event, NameOfService, serviceScope.ServiceProvider, 
                   cancellationToken
                );
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    #endregion

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
    
    /*---------------------------------------------------------------*/

    private void _EventPublishHandler(IModel channel, Event @event)
    {
        var nameOfEvent = @event.Type;

        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
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

    private void _MessageOfQueueHandle<TMessage>(IModel channel, BasicDeliverEventArgs args, TMessage message,
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

            if (_IsMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        messageBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                    
                    afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new object[] { message });
                }
                
                _TrySendAckMessage(channel, args);
            }
            else
            {
                if (messageBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message });

                    unitOfWork.Commit();
                }
                else
                    messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message });

                _CleanCache(messageBusHandlerMethod, serviceProvider);
            
                _TrySendAckMessage(channel, args);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _MessageOfQueueHandleAsync<TMessage>(IModel channel, BasicDeliverEventArgs args, TMessage message,
        IServiceProvider serviceProvider, CancellationToken cancellationToken
    ) where TMessage : class
    {
        ICoreUnitOfWork unitOfWork = null;

        try
        {
            var messageBusHandler     = serviceProvider.GetRequiredService(typeof(IConsumerMessageBusHandler<TMessage>));
            var messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            if (_IsMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        messageBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                    
                    await (Task)afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });
                }
                
                _TrySendAckMessage(channel, args);
            }
            else
            {
                if (messageBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });

                    unitOfWork.Commit();
                }
                else
                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });

                _CleanCache(messageBusHandlerMethod, serviceProvider);
            
                _TrySendAckMessage(channel, args);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _MessageOfQueueHandle(IModel channel, BasicDeliverEventArgs args, object message, 
        IServiceProvider serviceProvider
    )
    {
       ICoreUnitOfWork unitOfWork = null;

        try
        {
            var consumerMessageBusHandlerContract =
                typeof(IConsumerMessageBusHandler<>).MakeGenericType(message.GetType());
                
            var messageBusHandler     = serviceProvider.GetRequiredService(consumerMessageBusHandlerContract);
            var messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            if (_IsMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        messageBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                    
                    afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new[] { message });
                }
                
                _TrySendAckMessage(channel, args);
            }
            else
            {
                if (messageBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message });

                    unitOfWork.Commit();
                }
                else
                    messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message });

                _CleanCache(messageBusHandlerMethod, serviceProvider);
            
                _TrySendAckMessage(channel, args);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _MessageOfQueueHandleAsync(IModel channel, BasicDeliverEventArgs args, object message, 
        IServiceProvider serviceProvider, CancellationToken cancellationToken
    )
    {
       ICoreUnitOfWork unitOfWork = null;

        try
        {
            var consumerMessageBusHandlerContract =
                typeof(IConsumerMessageBusHandler<>).MakeGenericType(message.GetType());
                
            var messageBusHandler     = serviceProvider.GetRequiredService(consumerMessageBusHandlerContract);
            var messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            if (_IsMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        messageBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                    
                    await (Task)afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new[] { message, cancellationToken });
                }
                
                _TrySendAckMessage(channel, args);
            }
            else
            {
                if (messageBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message, cancellationToken });

                    unitOfWork.Commit();
                }
                else
                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message, cancellationToken });

                _CleanCache(messageBusHandlerMethod, serviceProvider);
            
                _TrySendAckMessage(channel, args);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _EventOfQueueHandle(IModel channel, BasicDeliverEventArgs args, Event @event, string service,
        IServiceProvider serviceProvider
    )
    {
        Type eventBusHandlerType   = null;
        ICoreUnitOfWork unitOfWork = null;

        try
        {
            var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

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

                if (_IsMaxRetryMessage(args, retryAttr))
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            eventBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                        
                        afterMaxRetryHandlerMethod.Invoke(eventBusHandler, new[] { payload });
                    }
                    
                    _TrySendAckMessage(channel, args);
                }
                else
                {
                    if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                        unitOfWork.Transaction(transactionAttr.IsolationLevel);

                        eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload });

                        unitOfWork.Commit();
                    }
                    else
                        eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload });

                    _CleanCache(eventBusHandlerMethod, serviceProvider);
                    
                    _TrySendAckMessage(channel, args);
                }
            }
            else
                _TrySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, service, 
                eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );

            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _EventOfQueueHandleAsync(IModel channel, BasicDeliverEventArgs args, Event @event, 
        string service, IServiceProvider serviceProvider, CancellationToken cancellationToken
    )
    {
        Type eventBusHandlerType   = null;
        ICoreUnitOfWork unitOfWork = null;

        try
        {
            var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

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
                    eventBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");

                var retryAttr =
                    eventBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

                if (_IsMaxRetryMessage(args, retryAttr))
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            eventBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                        
                        await (Task)afterMaxRetryHandlerMethod.Invoke(eventBusHandler, new[] { payload, cancellationToken });
                    }
                    
                    _TrySendAckMessage(channel, args);
                }
                else
                {
                    if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;
                    
                        unitOfWork.Transaction(transactionAttr.IsolationLevel);

                        await (Task)eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload, cancellationToken });

                        unitOfWork.Commit();
                    }
                    else
                        await (Task)eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload, cancellationToken });

                    _CleanCache(eventBusHandlerMethod, serviceProvider);
                    
                    _TrySendAckMessage(channel, args);
                }
            }
            else
                _TrySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, service, 
                eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );

            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _RequeueMessageAsDeadLetter(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            channel.BasicNack(args.DeliveryTag, false, false); //or _channel.BasicReject(args.DeliveryTag, false);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }
    
    private bool _IsMaxRetryMessage(BasicDeliverEventArgs args, WithMaxRetryAttribute maxRetryAttribute)
    {
        var xDeath = args.BasicProperties.Headers?.FirstOrDefault(header => header.Key.Equals("x-death")).Value;

        var xDeathInfo = (xDeath as List<object>)?.FirstOrDefault() as Dictionary<string, object>;
                
        var countRetry = xDeathInfo?.FirstOrDefault(header => header.Key.Equals("count")).Value;

        return Convert.ToInt32(countRetry) > maxRetryAttribute?.Count;
    }
    
    private void _TrySendAckMessage(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            channel.BasicAck(args.DeliveryTag, false); //Delete this message from queue
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }
    
    private Type _GetTypeOfUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork))
        ) ?? domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private Type _GetTypeOfCommandUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private void _CleanCache(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider)
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
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }
}