#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Domic.Core.Common.ClassConsts;
using Domic.Core.Common.ClassEnums;
using Domic.Core.Common.ClassModels;
using Domic.Core.Domain.Attributes;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NSubstitute.Exceptions;
using Polly;
using Environment = System.Environment;

namespace Domic.Core.Infrastructure.Concretes;

public class EventStreamBroker(
    ISerializer serializer, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment, IDateTime dateTime,
    IGlobalUniqueIdGenerator globalUniqueIdGenerator, IServiceScopeFactory serviceScopeFactory,
    IConfiguration configuration
) : IEventStreamBroker
{
    private static object _lock = new();
    private static SemaphoreSlim _asyncLock = new(1, 1);
    
    #region Consts

    private const string CountOfRetryKey = "CountOfRetry";

    #endregion
    
    public string NameOfAction { get; set; }
    public string NameOfService { get; set; }

    #region MessageStructure

    public void Publish<TMessage>(string topic, TMessage message, Dictionary<string, string> headers = default)
        where TMessage : class
    {
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        var kafkaHeaders = new Headers();

        foreach (var header in headers)
            kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));

        Policy.Handle<Exception>()
              .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
              .Execute(() => {
                  
                  using var producer = new ProducerBuilder<string, string>(config).Build();
                  
                  producer.Produce(
                      topic,
                      new Message<string, string> {
                          Key = message.GetType().Name, Value = serializer.Serialize(message), Headers = kafkaHeaders
                      }
                  );
                  
              });
    }

    public Task PublishAsync<TMessage>(string topic, TMessage message, Dictionary<string, string> headers = default,
        CancellationToken cancellationToken = default
    ) where TMessage : class
    {
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        var kafkaHeaders = new Headers();

        foreach (var header in headers)
            kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));

        return Policy.Handle<Exception>()
                     .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                     .ExecuteAsync(async () => {
                         
                         using var producer = new ProducerBuilder<string, string>(config).Build();

                         await producer.ProduceAsync(
                             topic,
                             new Message<string, string> {
                                 Key = message.GetType().Name, Value = serializer.Serialize(message), Headers = kafkaHeaders
                             },
                             cancellationToken: cancellationToken
                         );
                         
                     });
    }
    
    public void SubscribeMessage(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                
                _ConsumeNextMessage(useCaseTypes, topic, consumer, consumeResult);
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }
    
    public void SubscribeRetriableMessage(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                _ConsumeNextRetriableMessage(useCaseTypes, topic, consumer, consumeResult);
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }

    public void SubscribeMessageAsynchronously(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        #region ThrottleConfigs

        List<Task> consumerTasks = new();
        
        var topicConfig = configuration.GetSection("ExternalTopicConfig").Get<TopicConfig>();

        var topicThrottle = topicConfig.Throttle.FirstOrDefault(throttle => throttle.Topic.Equals(topic));

        #endregion

        while (!cancellationToken.IsCancellationRequested)
        {
            #region ThrottleConditions

            if (topicThrottle.Active)
            {
                if (consumerTasks.Count == topicThrottle.Limitation && !consumerTasks.All(task => task.IsCompleted))
                {
                    Thread.Sleep(50); //busy waiting
                    continue;
                }
            
                if(consumerTasks.All(task => task.IsCompleted))
                    consumerTasks.RemoveAll(task => task.IsCompleted);
            }

            #endregion

            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                var consumerTask =
                    _ConsumeNextMessageAsync(useCaseTypes, topic, consumer, consumeResult, cancellationToken);
                
                consumerTasks.Add(consumerTask);
            }
            catch (Exception e)
            {
                //fire&forget
                e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }
    
    public void SubscribeRetriableMessageAsynchronously(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);
        
        #region ThrottleConfigs

        List<Task> consumerTasks = new();
        
        var topicConfig = configuration.GetSection("ExternalTopicConfig").Get<TopicConfig>();

        var topicThrottle = topicConfig.Throttle.FirstOrDefault(throttle => throttle.Topic.Equals(topic));

        #endregion

        while (!cancellationToken.IsCancellationRequested)
        {
            #region ThrottleConditions

            if (consumerTasks.Count == topicThrottle.Limitation && !consumerTasks.All(task => task.IsCompleted))
            {
                Thread.Sleep(50); //busy waiting
                continue;
            }

            //reset
            if (consumerTasks.All(task => task.IsCompleted))
            {
                consumerTasks.RemoveAll(task => task.IsCompleted);
                Thread.Sleep(5000); //5s
            }

            #endregion
            
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                var consumerTask =
                    _ConsumeNextRetriableMessageAsync(useCaseTypes, topic, consumer, consumeResult, cancellationToken);
                
                consumerTasks.Add(consumerTask);
            }
            catch (Exception e)
            {
                //fire&forget
                e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
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
            using IServiceScope serviceScope = serviceScopeFactory.CreateScope();

            var commandUnitOfWork =
                serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Command)) as ICoreCommandUnitOfWork;

            var redisCache = serviceScope.ServiceProvider.GetRequiredService<IInternalDistributedCache>();
            var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();
            
            try
            {
                var eventLocks = new List<string>();
                
                commandUnitOfWork.Transaction();

                var events =
                    eventCommandRepository.FindAllWithOrderingAsync(Order.Date, cancellationToken: cancellationToken)
                                          .GetAwaiter()
                                          .GetResult();
                
                foreach (Event targetEvent in events)
                {
                    #region DistributedLock

                    var lockEventKey = $"LockEventId-{targetEvent.Id}";
                    
                    //ReleaseDistributedLock
                    redisCache.DeleteKey(lockEventKey);
                    
                    //AcquireDistributedLock
                    var lockEventSuccessfully = redisCache.SetCacheValue(
                        new KeyValuePair<string, string>(lockEventKey, targetEvent.Id), CacheSetType.NotExists
                    );

                    #endregion

                    if (lockEventSuccessfully)
                    {
                        eventLocks.Add(lockEventKey);
                        
                        if (targetEvent.IsActive == IsActive.Active)
                        {
                            _EventPublishHandler(targetEvent);

                            var nowDateTime        = DateTime.Now;
                            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);

                            targetEvent.IsActive              = IsActive.InActive;
                            targetEvent.UpdatedAt_EnglishDate = nowDateTime;
                            targetEvent.UpdatedAt_PersianDate = nowPersianDateTime;

                            eventCommandRepository.Change(targetEvent);
                        }
                        else
                            eventCommandRepository.Remove(targetEvent);
                    }
                }

                commandUnitOfWork.Commit();
                
                //ReleaseDistributedLocks
                eventLocks.ForEach(@event => redisCache.DeleteKey(@event));
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);
                
                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                    NameOfService, NameOfAction
                );
                
                e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                    NameOfAction
                );

                _TryRollback(commandUnitOfWork);
            }
        }
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        //just one worker ( Task ) in current machine ( instance ) can process outbox events => lock

        await _asyncLock.WaitAsync(cancellationToken);
        
        //ScopeServices Trigger
        using IServiceScope serviceScope = serviceScopeFactory.CreateAsyncScope();

        var commandUnitOfWork =
            serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfUnitOfWork(TransactionType.Command)) as ICoreCommandUnitOfWork;

        var redisCache = serviceScope.ServiceProvider.GetRequiredService<IInternalDistributedCache>();
        var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();

        try
        {
            var eventLocks = new List<string>();

            await commandUnitOfWork.TransactionAsync(cancellationToken: cancellationToken);

            var events =
                await eventCommandRepository.FindAllWithOrderingAsync(Order.Date, cancellationToken: cancellationToken);

            foreach (Event targetEvent in events)
            {
                #region DistributedLock

                var lockEventKey = $"LockEventId-{targetEvent.Id}";

                //ReleaseDistributedLock
                await redisCache.DeleteKeyAsync(lockEventKey, cancellationToken);

                //AcquireDistributedLock
                var lockEventSuccessfully = await redisCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(lockEventKey, targetEvent.Id), CacheSetType.NotExists, 
                    cancellationToken: cancellationToken
                );

                #endregion

                if (lockEventSuccessfully)
                {
                    eventLocks.Add(lockEventKey);

                    if (targetEvent.IsActive == IsActive.Active)
                    {
                        await _EventPublishHandlerAsync(targetEvent, cancellationToken);

                        var nowDateTime = DateTime.Now;
                        var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);

                        targetEvent.IsActive = IsActive.InActive;
                        targetEvent.UpdatedAt_EnglishDate = nowDateTime;
                        targetEvent.UpdatedAt_PersianDate = nowPersianDateTime;

                        eventCommandRepository.Change(targetEvent);
                    }
                    else
                        eventCommandRepository.Remove(targetEvent);
                }
            }

            await commandUnitOfWork.CommitAsync(cancellationToken);

            //ReleaseDistributedLocks
            foreach (var eventlock in eventLocks)
                await redisCache.DeleteKeyAsync(eventlock, cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );

            //fire&forget
            e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, this, dateTime,
                NameOfService, NameOfAction, cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(commandUnitOfWork, cancellationToken);
        }
        finally
        {
            _asyncLock.Release();
        }
    }

    public void Subscribe(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                
                _ConsumeNextEvent(useCaseTypes, topic, consumer, consumeResult);
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }

    public void SubscribeRetriable(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);
                
                _ConsumeNextRetriableEvent(useCaseTypes, topic, consumer, consumeResult);
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }

    public void SubscribeAsynchronously(string topic, CancellationToken cancellationToken)
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        #region ThrottleConfigs

        List<Task> consumerTasks = new();
        
        var topicConfig = configuration.GetSection("ExternalTopicConfig").Get<TopicConfig>();

        var topicThrottle = topicConfig.Throttle.FirstOrDefault(throttle => throttle.Topic.Equals(topic));

        #endregion

        while (!cancellationToken.IsCancellationRequested)
        {
            #region ThrottleConditions

            if (topicThrottle.Active)
            {
                if (consumerTasks.Count == topicThrottle.Limitation && !consumerTasks.All(task => task.IsCompleted))
                {
                    Thread.Sleep(50); //busy waiting
                    continue;
                }
            
                if(consumerTasks.All(task => task.IsCompleted))
                    consumerTasks.RemoveAll(task => task.IsCompleted);
            }

            #endregion

            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                var consumerTask =
                    _ConsumeNextEventAsync(useCaseTypes, topic, consumer, consumeResult, cancellationToken);
                
                consumerTasks.Add(consumerTask);
            }
            catch (Exception e)
            {
                //fire&forget
                e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }

    public void SubscribeRetriableAsynchronously(string topic, CancellationToken cancellationToken)
    {
         var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            GroupId = $"{NameOfService}-{topic}"
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);
        
        #region ThrottleConfigs

        List<Task> consumerTasks = new();
        
        var topicConfig = configuration.GetSection("ExternalTopicConfig").Get<TopicConfig>();

        var topicThrottle = topicConfig.Throttle.FirstOrDefault(throttle => throttle.Topic.Equals(topic));

        #endregion

        while (!cancellationToken.IsCancellationRequested)
        {
            #region ThrottleConditions

            if (consumerTasks.Count == topicThrottle.Limitation && !consumerTasks.All(task => task.IsCompleted))
            {
                Thread.Sleep(50); //busy waiting
                continue;
            }

            //reset
            if (consumerTasks.All(task => task.IsCompleted))
            {
                consumerTasks.RemoveAll(task => task.IsCompleted);
                Thread.Sleep(5000); //5s
            }

            #endregion
            
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                var consumerTask =
                    _ConsumeNextRetriableEventAsync(useCaseTypes, topic, consumer, consumeResult, cancellationToken);
                
                consumerTasks.Add(consumerTask);
            }
            catch (Exception e)
            {
                //fire&forget
                e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );
            }
        }
    }

    #endregion
    
    /*---------------------------------------------------------------*/

    private void _ConsumeNextMessage(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult
    )
    {
        IUnitOfWork unitOfWork = default;
        object payload = default;
        Type messageStreamHandlerType = default;
        
        try
        {
            var targetConsumerMessageStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerMessageStreamHandlerType is not null)
            {
                var messageStreamType =
                    targetConsumerMessageStreamHandlerType.GetInterfaces()
                                                          .Select(i => i.GetGenericArguments()[0])
                                                          .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerMessageStreamHandler<>).MakeGenericType(messageStreamType);

                var messageStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                messageStreamHandlerType = messageStreamHandler.GetType();

                payload = JsonConvert.DeserializeObject(consumeResult.Message.Value, messageStreamType);

                var messageStreamHandlerMethod =
                    messageStreamHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
                
                var transactionConfig =
                        messageStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    payload.GetType().GetProperty("Id",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(payload);
    
                //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(messageId, default).GetAwaiter().GetResult();

                if (consumerEventQuery is null)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                    unitOfWork.Transaction(transactionConfig.IsolationLevel);
                     
                    #region IdempotentConsumerPattern
    
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = consumeResult.Message.Key,
                        CountOfRetry = 0,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion

                    messageStreamHandlerMethod.Invoke(messageStreamHandler, new[] { payload });

                    unitOfWork.Commit();
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                messageStreamHandlerType is not null ? messageStreamHandlerType.Name : NameOfAction
            );

            _TryRollback(unitOfWork);
            
            var isSuccessRetry = _RetryEventOrMessageOfTopic($"{NameOfService}-Retry-{topic}", payload, countOfRetry: 1);

            if (isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private void _ConsumeNextRetriableMessage(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult
    )
    {
        IUnitOfWork unitOfWork = default;
        object payload = default;
        Type messageStreamHandlerType = default;

        var countOfRetryValue = Convert.ToInt32(
            Encoding.UTF8.GetString(
                consumeResult.Message.Headers.First(h => h.Key == CountOfRetryKey).GetValueBytes()
            )
        );
        
        try
        {
            var targetConsumerMessageStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerMessageStreamHandlerType is not null)
            {
                var messageStreamType =
                    targetConsumerMessageStreamHandlerType.GetInterfaces()
                                                          .Select(i => i.GetGenericArguments()[0])
                                                          .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerMessageStreamHandler<>).MakeGenericType(messageStreamType);

                var messageStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                messageStreamHandlerType = messageStreamHandler.GetType();

                payload = JsonConvert.DeserializeObject(consumeResult.Message.Value, messageStreamType);

                var messageStreamHandlerMethod =
                    messageStreamHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");

                var retryAttr =
                    messageStreamHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;
                 
                if (countOfRetryValue > retryAttr.Count)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            messageStreamHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                 
                        afterMaxRetryHandlerMethod.Invoke(messageStreamHandler, new[] { payload });
                    }
                }
                else
                {
                    var transactionConfig =
                        messageStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");

                    var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                    var messageId =
                        payload.GetType().GetProperty("Id",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                        ).GetValue(payload);
        
                    //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                    var consumerEventQuery =
                        consumerEventQueryRepository.FindByIdAsync(messageId, default).GetAwaiter().GetResult();

                    if (consumerEventQuery is null)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                        unitOfWork.Transaction(transactionConfig.IsolationLevel);
                         
                        #region IdempotentConsumerPattern
        
                        var nowDateTime = DateTime.Now;

                        consumerEventQuery = new ConsumerEventQuery {
                            Id = messageId.ToString(),
                            Type = consumeResult.Message.Key,
                            CountOfRetry = countOfRetryValue,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventQueryRepository.Add(consumerEventQuery);

                        #endregion

                        messageStreamHandlerMethod.Invoke(messageStreamHandler, new[] { payload });

                        unitOfWork.Commit();
                    }
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                messageStreamHandlerType is not null ? messageStreamHandlerType.Name : NameOfAction
            );
            
            _TryRollback(unitOfWork);
            
            countOfRetryValue++;
            
            var isSuccessRetry = _RetryEventOrMessageOfTopic($"{NameOfService}-Retry-{topic}", payload, countOfRetryValue);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private async Task _ConsumeNextMessageAsync(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken
    )
    {
        IUnitOfWork unitOfWork = default;
        object payload = default;
        Type messageStreamHandlerType = default;
        
        try
        {
            var targetConsumerMessageStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerMessageStreamHandlerType is not null)
            {
                var messageStreamType =
                    targetConsumerMessageStreamHandlerType.GetInterfaces()
                                                          .Select(i => i.GetGenericArguments()[0])
                                                          .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerMessageStreamHandler<>).MakeGenericType(messageStreamType);

                var messageStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                messageStreamHandlerType = messageStreamHandler.GetType();

                payload = JsonConvert.DeserializeObject(consumeResult.Message.Value, messageStreamType);

                var messageStreamHandlerMethod =
                    messageStreamHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                
                var transactionConfig =
                        messageStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                var messageId =
                    payload.GetType().GetProperty("Id",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                    ).GetValue(payload);
    
                var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(messageId, cancellationToken);

                if (consumerEventQuery is null)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                    await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                     
                    #region IdempotentConsumerPattern
    
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = messageId.ToString(),
                        Type = consumeResult.Message.Key,
                        CountOfRetry = 0,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion

                    await (Task)messageStreamHandlerMethod.Invoke(messageStreamHandler, new[] { payload, cancellationToken });

                    await unitOfWork.CommitAsync(cancellationToken);
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                messageStreamHandlerType is not null ? messageStreamHandlerType.Name : NameOfAction, 
                cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);
            
            var isSuccessRetry =
                await _RetryEventOrMessageOfTopicAsync($"{NameOfService}-Retry-{topic}", payload, countOfRetry: 1, cancellationToken);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private async Task _ConsumeNextRetriableMessageAsync(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken
    )
    {
        IUnitOfWork unitOfWork = default;
        object payload = default;
        Type messageStreamHandlerType = default;
        
        var countOfRetryValue = Convert.ToInt32(
            Encoding.UTF8.GetString(
                consumeResult.Message.Headers.First(h => h.Key == CountOfRetryKey).GetValueBytes()
            )
        );
        
        try
        {
             var targetConsumerMessageStreamHandlerType = useCaseTypes.FirstOrDefault(
                 type => type.GetInterfaces().Any(
                     i => i.IsGenericType &&
                          i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>) &&
                          i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                 )
             );

            if (targetConsumerMessageStreamHandlerType is not null)
            {
                var messageStreamType =
                    targetConsumerMessageStreamHandlerType.GetInterfaces()
                                                          .Select(i => i.GetGenericArguments()[0])
                                                          .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerMessageStreamHandler<>).MakeGenericType(messageStreamType);

                var messageStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                messageStreamHandlerType = messageStreamHandler.GetType();

                payload = JsonConvert.DeserializeObject(consumeResult.Message.Value, messageStreamType);

                var messageStreamHandlerMethod =
                    messageStreamHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");

                var retryAttr =
                    messageStreamHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;
                 
                if (countOfRetryValue > retryAttr.Count)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            messageStreamHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                 
                        await (Task)afterMaxRetryHandlerMethod.Invoke(messageStreamHandler, new[] { payload, cancellationToken });
                    }
                }
                else
                {
                    var transactionConfig =
                        messageStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");

                    var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();

                    var messageId =
                        payload.GetType().GetProperty("Id",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
                        ).GetValue(payload);
        
                    var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(messageId, cancellationToken);

                    if (consumerEventQuery is null)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                        await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                         
                        #region IdempotentConsumerPattern
        
                        var nowDateTime = DateTime.Now;

                        consumerEventQuery = new ConsumerEventQuery {
                            Id = messageId.ToString(),
                            Type = consumeResult.Message.Key,
                            CountOfRetry = countOfRetryValue,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventQueryRepository.Add(consumerEventQuery);

                        #endregion

                        await (Task)messageStreamHandlerMethod.Invoke(messageStreamHandler, new[] { payload, cancellationToken });

                        await unitOfWork.CommitAsync(cancellationToken);
                    }
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                messageStreamHandlerType is not null ? messageStreamHandlerType.Name : NameOfAction,
                cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);

            countOfRetryValue++;
            
            var isSuccessRetry =
                await _RetryEventOrMessageOfTopicAsync($"{NameOfService}-Retry-{topic}", payload, countOfRetryValue, cancellationToken);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    /*---------------------------------------------------------------*/
    
    private void _EventPublishHandler(Event @event)
    {
        var nameOfEvent = @event.Type;

        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
        var typeOfEvents = domainTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false
        );

        var typeOfEvent = typeOfEvents.FirstOrDefault(type => type.Name.Equals(nameOfEvent));

        var broker = typeOfEvent.GetCustomAttribute(typeof(MessageBrokerAttribute)) as MessageBrokerAttribute;
        
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        using var producer = new ProducerBuilder<string, string>(config).Build();
                 
        producer.Produce(
            broker.Topic,
            new Message<string, string> {
                Key = nameOfEvent, Value = serializer.Serialize(@event)
            }
        );
    }
    
    private async Task _EventPublishHandlerAsync(Event @event, CancellationToken cancellationToken)
    {
        var nameOfEvent = @event.Type;

        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
        var typeOfEvents = domainTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false
        );

        var typeOfEvent = typeOfEvents.FirstOrDefault(type => type.Name.Equals(nameOfEvent));

        var broker = typeOfEvent.GetCustomAttribute(typeof(MessageBrokerAttribute)) as MessageBrokerAttribute;
        
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        using var producer = new ProducerBuilder<string, string>(config).Build();
                 
        await producer.ProduceAsync(
            broker.Topic,
            new Message<string, string> {
                Key = nameOfEvent, Value = serializer.Serialize(@event)
            },
            cancellationToken: cancellationToken
        );
    }
    
    private void _ConsumeNextEvent(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult
    )
    {
        IUnitOfWork unitOfWork = default;
        Event @event = default;
        Type eventStreamHandlerType = default;
        
        try
        {
            var targetConsumerEventStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerEventStreamHandlerType is not null)
            {
                var eventStreamType =
                    targetConsumerEventStreamHandlerType.GetInterfaces()
                                                        .Select(i => i.GetGenericArguments()[0])
                                                        .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerEventStreamHandler<>).MakeGenericType(eventStreamType);

                var eventStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                eventStreamHandlerType = eventStreamHandler.GetType();

                @event = JsonConvert.DeserializeObject<Event>(consumeResult.Message.Value);

                var payload = JsonConvert.DeserializeObject(@event.Payload, eventStreamType);
                
                var eventStreamHandlerMethod =
                    eventStreamHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
                
                var transactionConfig =
                        eventStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
    
                //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(@event.Id, default).GetAwaiter().GetResult();

                if (consumerEventQuery is null)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                    unitOfWork.Transaction(transactionConfig.IsolationLevel);
                     
                    #region IdempotentConsumerPattern
    
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = @event.Id,
                        Type = consumeResult.Message.Key,
                        CountOfRetry = 0,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion

                    eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payload });

                    unitOfWork.Commit();
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                eventStreamHandlerType is not null ? eventStreamHandlerType.Name : NameOfAction
            );

            _TryRollback(unitOfWork);
            
            var isSuccessRetry = _RetryEventOrMessageOfTopic($"{NameOfService}-Retry-{topic}", @event, countOfRetry: 1);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private void _ConsumeNextRetriableEvent(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult
    )
    {
        IUnitOfWork unitOfWork = default;
        Event @event = default;
        Type eventStreamHandlerType = default;
        
        var countOfRetryValue = Convert.ToInt32(
            Encoding.UTF8.GetString(
                consumeResult.Message.Headers.First(h => h.Key == CountOfRetryKey).GetValueBytes()
            )
        );
        
        try
        {
            var targetConsumerEventStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerEventStreamHandlerType is not null)
            {
                var eventStreamType =
                    targetConsumerEventStreamHandlerType.GetInterfaces()
                                                        .Select(i => i.GetGenericArguments()[0])
                                                        .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerEventStreamHandler<>).MakeGenericType(eventStreamType);

                var eventStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                eventStreamHandlerType = eventStreamHandler.GetType();

                @event = JsonConvert.DeserializeObject<Event>(consumeResult.Message.Value);

                var payload = JsonConvert.DeserializeObject(@event.Payload, eventStreamType);
                
                var eventStreamHandlerMethod =
                    eventStreamHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
                
                var retryAttr =
                    eventStreamHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;
                 
                if (countOfRetryValue > retryAttr.Count)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            eventStreamHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                 
                        afterMaxRetryHandlerMethod.Invoke(eventStreamHandlerType, new[] { payload });
                    }
                }
                else
                {
                    var transactionConfig =
                        eventStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");

                    var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
    
                    //todo: should be used [CancelationToken] from this method ( _MessageOfQueueHandle )
                    var consumerEventQuery = consumerEventQueryRepository.FindByIdAsync(@event.Id, default).GetAwaiter().GetResult();

                    if (consumerEventQuery is null)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                        unitOfWork.Transaction(transactionConfig.IsolationLevel);
                     
                        #region IdempotentConsumerPattern
    
                        var nowDateTime = DateTime.Now;

                        consumerEventQuery = new ConsumerEventQuery {
                            Id = @event.Id,
                            Type = consumeResult.Message.Key,
                            CountOfRetry = 0,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventQueryRepository.Add(consumerEventQuery);

                        #endregion

                        eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payload });

                        unitOfWork.Commit();
                    }
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                eventStreamHandlerType is not null ? eventStreamHandlerType.Name : NameOfAction
            );

            _TryRollback(unitOfWork);
            
            countOfRetryValue++;
            
            var isSuccessRetry = _RetryEventOrMessageOfTopic($"{NameOfService}-Retry-{topic}", @event, countOfRetryValue);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private async Task _ConsumeNextEventAsync(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken
    )
    {
        IUnitOfWork unitOfWork = default;
        Event @event = default;
        Type eventStreamHandlerType = default;
        
        try
        {
            var targetConsumerEventStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerEventStreamHandlerType is not null)
            {
                var eventStreamType =
                    targetConsumerEventStreamHandlerType.GetInterfaces()
                                                        .Select(i => i.GetGenericArguments()[0])
                                                        .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerEventStreamHandler<>).MakeGenericType(eventStreamType);

                var eventStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                eventStreamHandlerType = eventStreamHandler.GetType();

                @event = JsonConvert.DeserializeObject<Event>(consumeResult.Message.Value);

                var payload = JsonConvert.DeserializeObject(@event.Payload, eventStreamType);
                
                var eventStreamHandlerMethod =
                    eventStreamHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                
                var transactionConfig =
                        eventStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                if(transactionConfig is null)
                    throw new Exception("Must be used transaction config attribute!");

                var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
    
                var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(@event.Id, cancellationToken);

                if (consumerEventQuery is null)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                    await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                     
                    #region IdempotentConsumerPattern
    
                    var nowDateTime = DateTime.Now;

                    consumerEventQuery = new ConsumerEventQuery {
                        Id = @event.Id,
                        Type = consumeResult.Message.Key,
                        CountOfRetry = 0,
                        CreatedAt_EnglishDate = nowDateTime,
                        CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                    };

                    consumerEventQueryRepository.Add(consumerEventQuery);

                    #endregion

                    await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payload, cancellationToken });

                    await unitOfWork.CommitAsync(cancellationToken);
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                eventStreamHandlerType is not null ? eventStreamHandlerType.Name : NameOfAction,
                cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);
            
            var isSuccessRetry = 
                await _RetryEventOrMessageOfTopicAsync($"{NameOfService}-Retry-{topic}", @event, countOfRetry: 1, cancellationToken);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
        }
    }
    
    private async Task _ConsumeNextRetriableEventAsync(Type[] useCaseTypes, string topic, IConsumer<string, string> consumer,
        ConsumeResult<string, string> consumeResult, CancellationToken cancellationToken
    )
    {
        IUnitOfWork unitOfWork = default;
        Event @event = default;
        Type eventStreamHandlerType = default;
        
        var countOfRetryValue = Convert.ToInt32(
            Encoding.UTF8.GetString(
                consumeResult.Message.Headers.First(h => h.Key == CountOfRetryKey).GetValueBytes()
            )
        );
        
        try
        {
            var targetConsumerEventStreamHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>) &&
                         i.GetGenericArguments().Any(arg => arg.Name.Equals(consumeResult.Message.Key))
                )
            );

            if (targetConsumerEventStreamHandlerType is not null)
            {
                var eventStreamType =
                    targetConsumerEventStreamHandlerType.GetInterfaces()
                                                        .Select(i => i.GetGenericArguments()[0])
                                                        .FirstOrDefault();

                var fullContractOfConsumerType =
                    typeof(IConsumerEventStreamHandler<>).MakeGenericType(eventStreamType);

                var eventStreamHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);

                eventStreamHandlerType = eventStreamHandler.GetType();

                @event = JsonConvert.DeserializeObject<Event>(consumeResult.Message.Value);

                var payload = JsonConvert.DeserializeObject(@event.Payload, eventStreamType);
                
                var eventStreamHandlerMethod =
                    eventStreamHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                
                var retryAttr =
                    eventStreamHandlerMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;
                 
                if (countOfRetryValue > retryAttr.Count)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            eventStreamHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                 
                        await (Task)afterMaxRetryHandlerMethod.Invoke(eventStreamHandlerType, new[] { payload, cancellationToken });
                    }
                }
                else
                {
                    var transactionConfig =
                        eventStreamHandlerMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;
        
                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");

                    var consumerEventQueryRepository = serviceProvider.GetRequiredService<IConsumerEventQueryRepository>();
    
                    var consumerEventQuery = await consumerEventQueryRepository.FindByIdAsync(@event.Id, cancellationToken);

                    if (consumerEventQuery is null)
                    {
                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionConfig.Type)) as IUnitOfWork;

                        await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);
                     
                        #region IdempotentConsumerPattern
    
                        var nowDateTime = DateTime.Now;

                        consumerEventQuery = new ConsumerEventQuery {
                            Id = @event.Id,
                            Type = consumeResult.Message.Key,
                            CountOfRetry = 0,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventQueryRepository.Add(consumerEventQuery);

                        #endregion

                        await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payload, cancellationToken });

                        await unitOfWork.CommitAsync(cancellationToken);
                    }
                }
            }
            
            _TryCommitOffset(consumer, consumeResult);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                NameOfService, NameOfAction
            );
            
            //fire&forget
            e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                eventStreamHandlerType is not null ? eventStreamHandlerType.Name : NameOfAction,
                cancellationToken: cancellationToken
            );

            await _TryRollbackAsync(unitOfWork, cancellationToken);

            countOfRetryValue++;
            
            var isSuccessRetry =
                await _RetryEventOrMessageOfTopicAsync($"{NameOfService}-Retry-{topic}", @event, countOfRetryValue, cancellationToken);
            
            if(isSuccessRetry)
                _TryCommitOffset(consumer, consumeResult);
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
            e.FileLogger(hostEnvironment, dateTime);
        }
    }
    
    private Task _TryRollbackAsync(IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        try
        {
            if (unitOfWork is not null)
                return Policy.Handle<Exception>()
                             .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                             .ExecuteAsync(() => unitOfWork.RollbackAsync(cancellationToken));
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);
        }

        return Task.CompletedTask;
    }
    
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

    private void _TryCommitOffset(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult)
    {
        try
        {
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() => consumer.Commit(consumeResult));
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
        }
    }
    
    private bool _RetryEventOrMessageOfTopic(string topic, object payload, int countOfRetry)
    {
        try
        {
            var config = new ProducerConfig {
                BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
                SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
                SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
            };
            
            var kafkaHeaders = new Headers {
                { CountOfRetryKey, Encoding.UTF8.GetBytes( $"{countOfRetry}" ) }
            };
            
            Policy.Handle<Exception>()
                  .WaitAndRetry(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                  .Execute(() => {
                      
                      using var producer = new ProducerBuilder<string, string>(config).Build();

                      producer.Produce(
                          topic,
                          new Message<string, string> {
                              Key = payload.GetType().Name, Value = serializer.Serialize(payload), Headers = kafkaHeaders
                          }
                      );
                      
                  });
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                NameOfService, NameOfAction
            );

            return false;
        }

        return true;
    }
    
    private async Task<bool> _RetryEventOrMessageOfTopicAsync(string topic, object payload, int countOfRetry,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var config = new ProducerConfig {
                BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
                SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
                SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
            };
            
            var kafkaHeaders = new Headers {
                { CountOfRetryKey, Encoding.UTF8.GetBytes( $"{countOfRetry}" ) }
            };
            
            await Policy.Handle<Exception>()
                        .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3), (exception, timeSpan, context) => {})
                        .ExecuteAsync(async () => {

                            using var producer = new ProducerBuilder<string, string>(config).Build();
                
                            await producer.ProduceAsync(
                                topic,
                                new Message<string, string> {
                                    Key = payload.GetType().Name, Value = serializer.Serialize(payload), Headers = kafkaHeaders
                                },
                                cancellationToken
                            );

                        });
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                NameOfService, NameOfAction
            );

            return false;
        }

        return true;
    }
}