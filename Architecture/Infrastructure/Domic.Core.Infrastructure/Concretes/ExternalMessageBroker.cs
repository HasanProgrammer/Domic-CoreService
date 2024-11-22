#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using System.Reflection;
using System.Text;
using Domic.Core.Common.ClassConsts;
using Domic.Core.Common.ClassEnums;
using Domic.Core.Domain.Attributes;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Common.ClassModels;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NSubstitute.Exceptions;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Domic.Core.Infrastructure.Concretes;

public class ExternalMessageBroker : IExternalMessageBroker
{
    private static object _lock = new();
    private static SemaphoreSlim _asyncLock = new(1, 1);

    private readonly IConnection _connection;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDateTime _dateTime;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;
    private readonly IConfiguration _configuration;

    public ExternalMessageBroker(IConfiguration configuration, IHostEnvironment hostEnvironment, 
        IServiceScopeFactory serviceScopeFactory, IDateTime dateTime, IGlobalUniqueIdGenerator globalUniqueIdGenerator
    )
    {
        _hostEnvironment = hostEnvironment;
        _serviceScopeFactory = serviceScopeFactory;
        _dateTime = dateTime;
        _globalUniqueIdGenerator = globalUniqueIdGenerator;
        _configuration = configuration;

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
        Policy.Handle<Exception>()
              .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
              .Execute(() => {

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

            });
    }

    public Task PublishAsync<TMessage>(MessageBrokerDto<TMessage> messageBroker, CancellationToken cancellationToken) 
        where TMessage : class 
        => Policy.Handle<Exception>()
                 .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                 .ExecuteAsync(() => 
                     Task.Run(() => {
                         
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
                         
                     }, cancellationToken)
                 );

    public void Subscribe<TMessage>(string queue) where TMessage : class
    {
        try
        {
            var channel = _connection.CreateModel();
            
            EventingBasicConsumer consumer = new(channel);

            consumer.Received += (sender, args) => {
                
                //ScopeServices Trigger
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
                
                //ScopeServices Trigger
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
            
            #region Throttle

            var queueConfig = _configuration.GetSection("ExternalQueueConfig").Get<QueueConfig>();

            var queueThrottle = queueConfig.Throttle.FirstOrDefault(throttle => throttle.Queue.Equals(queue));
            
            if(queueThrottle is not null && queueThrottle.Active)
                channel.BasicQos(queueThrottle.Size, queueThrottle.Limitation, queueThrottle.IsGlobally);

            #endregion
            
            AsyncEventingBasicConsumer consumer = new(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices Trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<TMessage>();

                await _MessageOfQueueHandleAsync(channel, args, message, serviceScope.ServiceProvider, cancellationToken);
                
            };
            
            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
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
            
            #region Throttle

            var queueConfig = _configuration.GetSection("ExternalQueueConfig").Get<QueueConfig>();

            var queueThrottle = queueConfig.Throttle.FirstOrDefault(throttle => throttle.Queue.Equals(queue));
            
            if(queueThrottle is not null && queueThrottle.Active)
                channel.BasicQos(queueThrottle.Size, queueThrottle.Limitation, queueThrottle.IsGlobally);

            #endregion
            
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
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    #endregion

    #region EventStructure
    
    public void Publish(CancellationToken cancellationToken)
    {
        //just one worker ( Task ) in current machine ( instance ) can process outbox events => lock
        lock (_lock)
        {
            //ScopeServices Trigger
            using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();

            var commandUnitOfWork =
                serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfCommandUnitOfWork()) as ICoreCommandUnitOfWork;

            var distributedCache = serviceScope.ServiceProvider.GetRequiredService<IInternalDistributedCache>();
            var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();
            
            var eventLocks = new List<string>();

            try
            {
                using var channel = _connection.CreateModel();

                commandUnitOfWork.Transaction();

                var events =
                    eventCommandRepository.FindAllWithOrderingAsync(Order.Date, cancellationToken: cancellationToken)
                                          .GetAwaiter()
                                          .GetResult();

                foreach (Event targetEvent in events)
                {
                    #region DistributedLock

                    var lockEventKey = $"LockEventId-{targetEvent.Id}";

                    var lockEventSuccessfully =
                        _TryAcquireDistributedLock(distributedCache, lockEventKey, targetEvent.Id);

                    #endregion

                    if (lockEventSuccessfully)
                    {
                        eventLocks.Add(lockEventKey);

                        if (targetEvent.IsActive == IsActive.Active)
                        {
                            _EventPublishHandler(channel, targetEvent);

                            var nowDateTime = DateTime.Now;
                            var nowPersianDateTime = _dateTime.ToPersianShortDate(nowDateTime);

                            targetEvent.IsActive = IsActive.InActive;
                            targetEvent.UpdatedAt_EnglishDate = nowDateTime;
                            targetEvent.UpdatedAt_PersianDate = nowPersianDateTime;

                            eventCommandRepository.Change(targetEvent);
                        }
                        else
                            eventCommandRepository.Remove(targetEvent);
                    }
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

                _TryRollback(commandUnitOfWork);
            }
            finally
            {
                _TryReleaseDistributedLocks(distributedCache, eventLocks);
            }
        }
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        //just one worker ( Task ) in current machine ( instance ) can process outbox events => lock
        await _asyncLock.WaitAsync(cancellationToken);
        
        //ScopeServices Trigger
        using IServiceScope serviceScope = _serviceScopeFactory.CreateAsyncScope();

        var commandUnitOfWork =
            serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfCommandUnitOfWork()) as ICoreCommandUnitOfWork;

        var distributedCache = serviceScope.ServiceProvider.GetRequiredService<IInternalDistributedCache>();
        var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();
        
        var eventLocks = new List<string>();
        
        try
        {
            using var channel = _connection.CreateModel();
            
            await commandUnitOfWork.TransactionAsync(cancellationToken: cancellationToken);

            var events =
                await eventCommandRepository.FindAllWithOrderingAsync(Order.Date, cancellationToken: cancellationToken);
            
            foreach (Event targetEvent in events)
            {
                #region DistributedLock

                var lockEventKey = $"LockEventId-{targetEvent.Id}";
                
                var lockEventSuccessfully =
                    await _TryAcquireDistributedLockAsync(distributedCache, lockEventKey, targetEvent.Id,
                        cancellationToken
                    );

                #endregion

                if (lockEventSuccessfully)
                {
                    eventLocks.Add(lockEventKey);
                    
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
            }

            await commandUnitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService, 
                NameOfAction, cancellationToken
            );

            await _TryRollbackAsync(commandUnitOfWork, cancellationToken);
        }
        finally
        {
            await _TryReleaseDistributedLocksAsync(distributedCache, eventLocks, cancellationToken);
            
            _asyncLock.Release();
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
                
                _EventOfQueueHandle(channel, args, @event, serviceScope.ServiceProvider);
                
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
            
            #region Throttle

            var queueConfig = _configuration.GetSection("ExternalQueueConfig").Get<QueueConfig>();

            var queueThrottle = queueConfig.Throttle.FirstOrDefault(throttle => throttle.Queue.Equals(queue));
            
            if(queueThrottle is not null && queueThrottle.Active)
                channel.BasicQos(queueThrottle.Size, queueThrottle.Limitation, queueThrottle.IsGlobally);

            #endregion
            
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var @event = Encoding.UTF8.GetString(args.Body.ToArray()).DeSerialize<Event>();
                
               await _EventOfQueueHandleAsync(channel, args, @event, serviceScope.ServiceProvider, 
                   cancellationToken
               );
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
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

    public ValueTask DisposeAsync()
    {
        _connection.Close();
        _connection.Dispose();
        
        return ValueTask.CompletedTask;
    }

    /*---------------------------------------------------------------*/

    private void _MessageOfQueueHandle<TMessage>(IModel channel, BasicDeliverEventArgs args, TMessage message,
        IServiceProvider serviceProvider
    ) where TMessage : class
    {
        IUnitOfWork unitOfWork     = null;
        Type messageBusHandlerType = null;

        try
        {
            var messageBusHandler = serviceProvider.GetRequiredService(typeof(IConsumerMessageBusHandler<TMessage>));
            messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusBeforeHandlerMethod =
                messageBusHandlerType.GetMethod("BeforeHandle") ?? throw new Exception("BeforeHandle function not found !");
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
            
            var messageBusAfterHandlerMethod =
                messageBusHandlerType.GetMethod("AfterHandle") ?? throw new Exception("AfterHandle function not found !");
            
            _BeforeHandleMessage(messageBusBeforeHandlerMethod, messageBusHandler, message);

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
            
            if (maxRetryInfo.result)
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                    _AfterMaxRetryHandleMessage(messageBusHandlerType, messageBusHandler, message);
            }
            else
            {
                var transactionConfig =
                    messageBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
                
                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");

                unitOfWork =
                    serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    message.GetType().GetProperty("Id", 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(message);
                
                //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(messageId, default).GetAwaiter().GetResult();

                if (consumerEventQuery is null)
                {
                    unitOfWork.Transaction(transactionConfig.IsolationLevel);
                    
                    #region IdempotentConsumerPattern
                
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = nameof(message),
                        CountOfRetry = maxRetryInfo.countOfRetry,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion
                
                    messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message });
                
                    unitOfWork.Commit();

                    _AfterHandleMessage(messageBusAfterHandlerMethod, messageBusHandler, message);

                    _CleanCacheMessage(messageBusHandlerMethod, serviceProvider);
                }
            }
            
            _TrySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, messageBusHandlerType is not null ? messageBusHandlerType.Name : NameOfAction
            );
            
            _TryRollback(unitOfWork);

            _TryRequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _MessageOfQueueHandleAsync<TMessage>(IModel channel, BasicDeliverEventArgs args, TMessage message,
        IServiceProvider serviceProvider, CancellationToken cancellationToken
    ) where TMessage : class
    {
        IUnitOfWork unitOfWork     = null;
        Type messageBusHandlerType = null;

        try
        {
            var messageBusHandler = serviceProvider.GetRequiredService(typeof(IConsumerMessageBusHandler<TMessage>));
            messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusBeforeHandlerMethod =
                messageBusHandlerType.GetMethod("BeforeHandleAsync") ?? throw new Exception("BeforeHandleAsync function not found !");
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
            
            var messageBusAfterHandlerMethod =
                messageBusHandlerType.GetMethod("AfterHandleAsync") ?? throw new Exception("AfterHandleAsync function not found !");

            await _BeforeHandleMessageAsync(messageBusBeforeHandlerMethod, messageBusHandler, message,
                cancellationToken
            );
            
            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;
            
            var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
            
            if (maxRetryInfo.result)
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                    await _AfterMaxRetryHandleMessageAsync(messageBusHandlerType, messageBusHandler, message, 
                        cancellationToken
                    );
            }
            else
            {
                var transactionConfig =
                    messageBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");
                
                unitOfWork =
                    serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    message.GetType().GetProperty("Id", 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(message);
                
                var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(messageId, cancellationToken);

                if (consumerEventQuery is null)
                {
                    await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                
                    #region IdempotentConsumerPattern
                
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = nameof(message),
                        CountOfRetry = maxRetryInfo.countOfRetry,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion
                    
                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });
                
                    await unitOfWork.CommitAsync(cancellationToken);

                    await _AfterHandleMessageAsync(messageBusAfterHandlerMethod,
                        messageBusHandler, message, cancellationToken
                    );

                    await _CleanCacheMessageAsync(messageBusHandlerMethod, serviceProvider, cancellationToken);
                }
            }
            
            await _TrySendAckMessageAsync(channel, args, cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, messageBusHandlerType is not null ? messageBusHandlerType.Name : NameOfAction
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);
            
            await _TryRequeueMessageAsDeadLetterAsync(channel, args, cancellationToken);
        }
    }
    
    private void _MessageOfQueueHandle(IModel channel, BasicDeliverEventArgs args, object message, 
        IServiceProvider serviceProvider
    )
    {
        IUnitOfWork unitOfWork     = null;
        Type messageBusHandlerType = null;
       
        try
        {
            var messageType = message.GetType();
            
            var consumerMessageBusHandlerContract =
                typeof(IConsumerMessageBusHandler<>).MakeGenericType(messageType);
                
            var messageBusHandler = serviceProvider.GetRequiredService(consumerMessageBusHandlerContract);
            messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusBeforeHandlerMethod =
                messageBusHandlerType.GetMethod("BeforeHandle") ?? throw new Exception("BeforeHandle function not found !");
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
            
            var messageBusAfterHandlerMethod =
                messageBusHandlerType.GetMethod("AfterHandle") ?? throw new Exception("AfterHandle function not found !");
            
            _BeforeHandleMessage(messageBusBeforeHandlerMethod, messageBusHandler, message);

            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
            
            if (maxRetryInfo.result)
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                    _AfterMaxRetryHandleMessage(messageBusHandlerType, messageBusHandler, message);
            }
            else
            {
                var transactionConfig =
                    messageBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");
                
                unitOfWork =
                    serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    message.GetType().GetProperty("Id", 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(message);
                
                //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(messageId, default).GetAwaiter().GetResult();

                if (consumerEventQuery is null)
                {
                    unitOfWork.Transaction(transactionConfig.IsolationLevel);
                    
                    #region IdempotentConsumerPattern
                
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = messageType.Name,
                        CountOfRetry = maxRetryInfo.countOfRetry,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion
                
                    messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message });
                
                    unitOfWork.Commit();
                    
                    _AfterHandleMessage(messageBusAfterHandlerMethod, messageBusHandler, message);

                    _CleanCacheMessage(messageBusHandlerMethod, serviceProvider);
                }
            }
            
            _TrySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, messageBusHandlerType is not null ? messageBusHandlerType.Name : NameOfAction
            );
            
            _TryRollback(unitOfWork);

            _TryRequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _MessageOfQueueHandleAsync(IModel channel, BasicDeliverEventArgs args, object message, 
        IServiceProvider serviceProvider, CancellationToken cancellationToken
    )
    {
        IUnitOfWork unitOfWork     = null;
        Type messageBusHandlerType = null;
       
        try
        {
            var messageType = message.GetType();
            
            var consumerMessageBusHandlerContract =
                typeof(IConsumerMessageBusHandler<>).MakeGenericType(messageType);
                
            var messageBusHandler = serviceProvider.GetRequiredService(consumerMessageBusHandlerContract);
            messageBusHandlerType = messageBusHandler.GetType();
            
            var messageBusBeforeHandlerMethod =
                messageBusHandlerType.GetMethod("BeforeHandleAsync") ?? throw new Exception("BeforeHandleAsync function not found !");
            
            var messageBusHandlerMethod =
                messageBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
            
            var messageBusAfterHandlerMethod =
                messageBusHandlerType.GetMethod("AfterHandleAsync") ?? throw new Exception("AfterHandleAsync function not found !");

            await _BeforeHandleMessageAsync(messageBusBeforeHandlerMethod, messageBusHandler, message,
                cancellationToken
            );
            
            var retryAttr =
                messageBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
            
            if (maxRetryInfo.result)
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                    await _AfterMaxRetryHandleMessageAsync(messageBusHandlerType, messageBusHandler, message, 
                        cancellationToken
                    );
            }
            else
            {
                var transactionConfig =
                    messageBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");
                
                unitOfWork =
                    serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;
                
                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    message.GetType().GetProperty("Id", 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(message);
                
                var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(messageId, cancellationToken);

                if (consumerEventQuery is null)
                {
                    await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                
                    #region IdempotentConsumerPattern
                
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = messageType.Name,
                        CountOfRetry = maxRetryInfo.countOfRetry,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion
                
                    await (Task)messageBusHandlerMethod.Invoke(messageBusHandler, new[] { message, cancellationToken });
                
                    await unitOfWork.CommitAsync(cancellationToken);

                    await _AfterHandleMessageAsync(messageBusAfterHandlerMethod, messageBusHandler,
                        message, cancellationToken
                    );

                    await _CleanCacheMessageAsync(messageBusHandlerMethod, serviceProvider, cancellationToken);
                }
            }
            
            await _TrySendAckMessageAsync(channel, args, cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, messageBusHandlerType is not null ? messageBusHandlerType.Name : NameOfAction
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);
            
            await _TryRequeueMessageAsDeadLetterAsync(channel, args, cancellationToken);
        }
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

        var messageBroker = typeOfEvent.GetCustomAttribute(typeof(EventConfigAttribute)) as EventConfigAttribute;

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
    
    private void _EventOfQueueHandle(IModel channel, BasicDeliverEventArgs args, Event @event,
        IServiceProvider serviceProvider
    )
    {
        IUnitOfWork unitOfWork   = null;
        Type eventBusHandlerType = null;

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
                
                var eventBusBeforeHandlerMethod =
                    eventBusHandlerType.GetMethod("BeforeHandle") ?? throw new Exception("BeforeHandle function not found !");

                var eventBusHandlerMethod =
                    eventBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");

                var eventBusAfterHandlerMethod =
                    eventBusHandlerType.GetMethod("AfterHandle") ?? throw new Exception("AfterHandle function not found !");
                
                _BeforeHandleEvent(eventBusBeforeHandlerMethod, eventBusHandler, payload);
                
                var retryAttr =
                    eventBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

                var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
                
                if (maxRetryInfo.result)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                        _AfterMaxRetryHandleEvent(eventBusHandlerType, eventBusHandler, payload);
                }
                else
                {
                    var transactionConfig =
                        eventBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                    //for query side event processing
                    if (transactionConfig.Type == TransactionType.Query)
                    {
                        var consumerEventQueryRepository =
                            serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
                        
                        //todo: should be used [CancelationToken] from this method ( _EventOfQueueHandle )
                        var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(@event.Id, default).GetAwaiter().GetResult();
                        
                        if (consumerEventQuery is null)
                        {
                            unitOfWork =
                                serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Query)) as IUnitOfWork;

                            unitOfWork.Transaction(transactionConfig.IsolationLevel);

                            #region IdempotentConsumerPattern

                            var nowDateTime = DateTime.Now;
                        
                            consumerEventQuery = new ConsumerEventQuery {
                                Id = @event.Id,
                                Type = @event.Type,
                                CountOfRetry = maxRetryInfo.countOfRetry,
                                CreatedAt_EnglishDate = nowDateTime,
                                CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                            };
                                
                            consumerEventQueryRepository.Add(consumerEventQuery);

                            #endregion
            
                            eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload });

                            unitOfWork.Commit();
                            
                            _AfterHandleEvent(eventBusAfterHandlerMethod, eventBusHandler, payload);
        
                            _CleanCacheEvent(eventBusHandlerMethod, serviceProvider);
                        }
                    }
                    //for command side event processing
                    else if (transactionConfig.Type == TransactionType.Command)
                    {
                        var consumerEventCommandRepository =
                            serviceProvider.GetRequiredService<IConsumerEventCommandRepository>();

                        var consumerEventCommand = consumerEventCommandRepository.FindById(@event.Id);

                        if (consumerEventCommand is null)
                        {
                            unitOfWork =
                                serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Command)) as IUnitOfWork;

                            unitOfWork.Transaction(transactionConfig.IsolationLevel);

                            #region IdempotentConsumerPattern

                            var nowDateTime = DateTime.Now;

                            consumerEventCommand = new ConsumerEvent {
                                Id = @event.Id,
                                Type = @event.Type,
                                CountOfRetry = maxRetryInfo.countOfRetry,
                                CreatedAt_EnglishDate = nowDateTime,
                                CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                            };

                            consumerEventCommandRepository.Add(consumerEventCommand);

                            #endregion

                            eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload });

                            unitOfWork.Commit();
                            
                            _AfterHandleEvent(eventBusAfterHandlerMethod, eventBusHandler, payload);

                            _CleanCacheEvent(eventBusHandlerMethod, serviceProvider);
                        }
                    }
                    else 
                        throw new Exception("Must be defined transaction type!");
                }
            }
            
            _TrySendAckMessage(channel, args);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService, 
                eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );
            
            _TryRollback(unitOfWork);
            
            _TryRequeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private async Task _EventOfQueueHandleAsync(IModel channel, BasicDeliverEventArgs args, Event @event, 
        IServiceProvider serviceProvider, CancellationToken cancellationToken
    )
    {
        Type eventBusHandlerType = null;
        IUnitOfWork unitOfWork   = null;

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
                
                var eventBusBeforeHandlerMethod =
                    eventBusHandlerType.GetMethod("BeforeHandleAsync") ?? throw new Exception("BeforeHandleAsync function not found !");

                var eventBusHandlerMethod =
                    eventBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                
                var eventBusAfterHandlerMethod =
                    eventBusHandlerType.GetMethod("AfterHandleAsync") ?? throw new Exception("AfterHandleAsync function not found !");

                await _BeforeHandleEventAsync(eventBusBeforeHandlerMethod, eventBusHandler, payload, cancellationToken);
                
                var retryAttr =
                    eventBusHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

                var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
                
                if (maxRetryInfo.result)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                        await _AfterMaxRetryHandleEventAsync(eventBusHandlerType, eventBusHandler, payload,
                            cancellationToken
                        );
                }
                else
                {
                    var transactionConfig =
                        eventBusHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                    //for query side event processing
                    if (transactionConfig.Type == TransactionType.Query)
                    {
                        var consumerEventQueryRepository =
                            serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
                        
                        var consumerEventQuery =
                            await consumerEventQueryRepository.FindByIdAsync(@event.Id, cancellationToken);
                        
                        if (consumerEventQuery is null)
                        {
                            unitOfWork =
                                serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Query)) as IUnitOfWork;

                            await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);

                            #region IdempotentConsumerPattern

                            var nowDateTime = DateTime.Now;
                        
                            consumerEventQuery = new ConsumerEventQuery {
                                Id = @event.Id,
                                Type = @event.Type,
                                CountOfRetry = maxRetryInfo.countOfRetry,
                                CreatedAt_EnglishDate = nowDateTime,
                                CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                            };
                                
                            consumerEventQueryRepository.Add(consumerEventQuery);

                            #endregion
            
                            await (Task)eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload, cancellationToken });

                            await unitOfWork.CommitAsync(cancellationToken);
                            
                            await _AfterHandleEventAsync(eventBusAfterHandlerMethod,
                                eventBusHandler, payload, cancellationToken
                            );
        
                            await _CleanCacheEventAsync(eventBusHandlerMethod, serviceProvider, cancellationToken);
                        }
                    }
                    //for command side event processing
                    else if (transactionConfig.Type == TransactionType.Command)
                    {
                        var consumerEventCommandRepository =
                            serviceProvider.GetRequiredService<IConsumerEventCommandRepository>();

                        var consumerEventCommand =
                            await consumerEventCommandRepository.FindByIdAsync(@event.Id, cancellationToken);

                        if (consumerEventCommand is null)
                        {
                            unitOfWork =
                                serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Command)) as IUnitOfWork;

                            await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);

                            #region IdempotentConsumerPattern

                            var nowDateTime = DateTime.Now;

                            consumerEventCommand = new ConsumerEvent {
                                Id = @event.Id,
                                Type = @event.Type,
                                CountOfRetry = maxRetryInfo.countOfRetry,
                                CreatedAt_EnglishDate = nowDateTime,
                                CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                            };

                            consumerEventCommandRepository.Add(consumerEventCommand);

                            #endregion

                            await (Task)eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload, cancellationToken });

                            await unitOfWork.CommitAsync(cancellationToken);

                            await _AfterHandleEventAsync(eventBusAfterHandlerMethod,
                                eventBusHandler, payload, cancellationToken
                            );

                            await _CleanCacheEventAsync(eventBusHandlerMethod, serviceProvider, cancellationToken);
                        }
                    }
                    else throw new Exception("Must be defined transaction type!");
                }
            }
            
            await _TrySendAckMessageAsync(channel, args, cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                eventBusHandlerType is not null ? eventBusHandlerType.Name : NameOfAction,
                cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);
            
            await _TryRequeueMessageAsDeadLetterAsync(channel, args, cancellationToken);
        }
    }
    
    /*---------------------------------------------------------------*/

    private void _TryRollback(IUnitOfWork unitOfWork)
    {
        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() => unitOfWork?.Rollback());
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
        }
    }
    
    private async Task _TryRollbackAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            if (unitOfWork is not null)
                await Policy.Handle<Exception>()
                            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                            .ExecuteAsync(() => unitOfWork.RollbackAsync(cancellationToken));
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken: cancellationToken);
        }
    }
    
    private void _TryRequeueMessageAsDeadLetter(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() =>
                      channel.BasicNack(args.DeliveryTag, false, false) //or _channel.BasicReject(args.DeliveryTag, false)
                  );
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-TryRequeueMessageAsDeadLetter"
            );
        }
    }
    
    private async Task _TryRequeueMessageAsDeadLetterAsync(IModel channel, BasicDeliverEventArgs args, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                        .ExecuteAsync(() =>
                            Task.Run(() => channel.BasicNack(args.DeliveryTag, false, false), cancellationToken)
                        );
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-TryRequeueMessageAsDeadLetter"
            );
        }
    }
    
    private void _TrySendAckMessage(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() =>
                      channel.BasicAck(args.DeliveryTag, false) //delete this message from queue
                  );
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-TrySendAckMessage"
            );
        }
    }
    
    private async Task _TrySendAckMessageAsync(IModel channel, BasicDeliverEventArgs args, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                        .ExecuteAsync(() =>
                            Task.Run(() => channel.BasicAck(args.DeliveryTag, false), cancellationToken)
                        );
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-TrySendAckMessage"
            );
        }
    }

    private bool _TryAcquireDistributedLock(IInternalDistributedCache distributedCache, string lockEventKey, 
        string lockEventValue
    )
    {
        bool result = false;

        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => { })
                  .Execute(() => {
                      
                      result = distributedCache.SetCacheValue(
                          new KeyValuePair<string, string>(lockEventKey, lockEventValue),
                          TimeSpan.FromMinutes(3),
                          CacheSetType.NotExists
                      );
                      
                  });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);

            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime,
                NameOfService, $"{NameOfAction}-TryAcquireDistributedLock"
            );

            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-TryAcquireDistributedLock"
            );
        }

        return result;
    }

    private void _TryReleaseDistributedLocks(IInternalDistributedCache distributedCache, List<string> locks)
    {
        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() =>
                      locks.ForEach(@lock => distributedCache.DeleteKey(@lock))
                  );
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);

            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime,
                NameOfService, $"{NameOfAction}-TryReleaseDistributedLocks"
            );

            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-TryReleaseDistributedLocks"
            );
        }
    }
    
    private async Task<bool> _TryAcquireDistributedLockAsync(IInternalDistributedCache distributedCache, string lockEventKey, 
        string lockEventValue, CancellationToken cancellationToken
    )
    {
        bool result = false;

        try
        {
            await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => { })
                        .ExecuteAsync(async () => {
                            
                            result = await distributedCache.SetCacheValueAsync(
                                new KeyValuePair<string, string>(lockEventKey, lockEventValue),
                                TimeSpan.FromMinutes(3),
                                CacheSetType.NotExists,
                                cancellationToken
                            );
                            
                        });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);

            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime,
                NameOfService, $"{NameOfAction}-TryAcquireDistributedLock"
            );

            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-TryAcquireDistributedLock", cancellationToken
            );
        }

        return result;
    }

    private async Task _TryReleaseDistributedLocksAsync(IInternalDistributedCache distributedCache, List<string> locks,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                        .ExecuteAsync(async () => {
                            
                            foreach (var @lock in locks)
                                await distributedCache.DeleteKeyAsync(@lock, cancellationToken);
                            
                        });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);

            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime,
                NameOfService, $"{NameOfAction}-TryReleaseDistributedLocks"
            );

            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-TryReleaseDistributedLocks", cancellationToken
            );
        }
    }
    
    private (bool result, int countOfRetry) _IsMaxRetryMessage(BasicDeliverEventArgs args, WithMaxRetryAttribute maxRetryAttribute)
    {
        var xDeath = args.BasicProperties.Headers?.FirstOrDefault(header => header.Key.Equals("x-death")).Value;

        var xDeathInfo = (xDeath as List<object>)?.FirstOrDefault() as Dictionary<string, object>;
                
        var countRetry = xDeathInfo?.FirstOrDefault(header => header.Key.Equals("count")).Value;

        return ( Convert.ToInt32(countRetry) > maxRetryAttribute?.Count , Convert.ToInt32(countRetry) );
    }
    
    /*---------------------------------------------------------------*/
    
    private Type _GetTypeOfUnitOfWork(TransactionType transactionType)
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return transactionType switch {
            TransactionType.Query => 
                domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork))),
            TransactionType.Command => 
                domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))),
            _ => throw new ArgumentNotFoundException("Must be defined transaction type!")
        };
    }
    
    private Type _GetTypeOfCommandUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private void _CleanCacheMessage(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider)
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
            {
                var redisCache = serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    redisCache.DeleteKey(key);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-CleanCacheConsumer"
            );
        }
    }
    
    private async Task _CleanCacheMessageAsync(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
            {
                var redisCache = serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    await redisCache.DeleteKeyAsync(key, cancellationToken);
            }
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-CleanCacheConsumer"
            );
        }
    }
    
    private void _CleanCacheEvent(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider)
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
            {
                var redisCache = serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    redisCache.DeleteKey(key);
            }
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-CleanCacheConsumer"
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-CleanCacheConsumer"
            );
        }
    }
    
    private async Task _CleanCacheEventAsync(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
            {
                var redisCache = serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    await redisCache.DeleteKeyAsync(key, cancellationToken);
            }
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-CleanCacheConsumer"
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-CleanCacheConsumer", cancellationToken
            );
        }
    }
    
    private void _BeforeHandleMessage<TMessage>(MethodInfo messageBusBeforeHandlerMethod, object messageBusHandler, 
        TMessage message
    ) where TMessage : class
    {
        try
        {
            messageBusBeforeHandlerMethod.Invoke(messageBusHandler, new object[] { message });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle"
            );
        }
    }
    
    private async Task _BeforeHandleMessageAsync<TMessage>(MethodInfo messageBusBeforeHandlerMethod, 
        object messageBusHandler, TMessage message, CancellationToken cancellationToken
    ) where TMessage : class
    {
        try
        {
            await (Task)messageBusBeforeHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken});
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle"
            );
        }
    }
    
    private void _AfterHandleMessage<TMessage>(MethodInfo messageBusAfterHandlerMethod, 
        object messageBusHandler, TMessage message
    ) where TMessage : class
    {
        try
        {
            messageBusAfterHandlerMethod.Invoke(messageBusHandler, new object[] { message });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterHandle"
            );
        }
    }
    
    private async Task _AfterHandleMessageAsync<TMessage>(MethodInfo messageBusAfterHandlerMethod, 
        object messageBusHandler, TMessage message, CancellationToken cancellationToken
    ) where TMessage : class
    {
        try
        {
            await (Task)messageBusAfterHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterHandle"
            );
        }
    }
    
    private void _AfterMaxRetryHandleMessage<TMessage>(Type messageBusHandlerType, object messageBusHandler, TMessage message)
        where TMessage : class
    {
        try
        {
            var afterMaxRetryHandlerMethod =
                messageBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                        
            afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new object[] { message });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle"
            );
        }
    }
    
    private async Task _AfterMaxRetryHandleMessageAsync<TMessage>(Type messageBusHandlerType, object messageBusHandler,
        TMessage message, CancellationToken cancellationToken
    ) where TMessage : class
    {
        try
        {
            var afterMaxRetryHandlerMethod =
                messageBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                        
            await (Task)afterMaxRetryHandlerMethod.Invoke(messageBusHandler, new object[] { message, cancellationToken });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle"
            );
        }
    }
    
    private void _BeforeHandleEvent(MethodInfo eventBusBeforeHandlerMethod, object eventBusHandler, object @event)
    {
        try
        {
            eventBusBeforeHandlerMethod.Invoke(eventBusHandler, new[] { @event });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle"
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle"
            );
        }
    }
    
    private async Task _BeforeHandleEventAsync(MethodInfo eventBusBeforeHandlerMethod, object eventBusHandler, 
        object @event, CancellationToken cancellationToken
    )
    {
        try
        {
            await (Task)eventBusBeforeHandlerMethod.Invoke(eventBusHandler, new[] { @event, cancellationToken});
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle"
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, 
                NameOfService, $"{NameOfAction}-BeforeHandle", cancellationToken
            );
        }
    }
    
    private void _AfterHandleEvent(MethodInfo eventBusAfterHandlerMethod, object eventBusHandler, 
        object @event
    )
    {
        try
        {
            eventBusAfterHandlerMethod.Invoke(eventBusHandler, new[] { @event });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterHandle"
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-AfterHandle"
            );
        }
    }
    
    private async Task _AfterHandleEventAsync(MethodInfo eventBusAfterHandlerMethod, object eventBusHandler,
        object @event, CancellationToken cancellationToken
    )
    {
        try
        {
            await (Task)eventBusAfterHandlerMethod.Invoke(eventBusHandler, new[] { @event, cancellationToken });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterHandle"
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, NameOfService,
                $"{NameOfAction}-AfterHandle", cancellationToken
            );
        }
    }
    
    private void _AfterMaxRetryHandleEvent(Type eventBusHandlerType, object eventBusHandler, object @event)
    {
        try
        {
            var afterMaxRetryHandlerMethod =
                eventBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                        
            afterMaxRetryHandlerMethod.Invoke(eventBusHandler, new[] { @event });
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle"
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle"
            );
        }
    }
    
    private async Task _AfterMaxRetryHandleEventAsync(Type eventBusHandlerType, object eventBusHandler,
        object @event, CancellationToken cancellationToken
    )
    {
        try
        {
            var afterMaxRetryHandlerMethod =
                eventBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                        
            await (Task)afterMaxRetryHandlerMethod.Invoke(eventBusHandler, new[] { @event, cancellationToken });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle"
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, this, _dateTime, 
                NameOfService, $"{NameOfAction}-AfterMaxRetryHandle", cancellationToken
            );
        }
    }
}