using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Domic.Core.Common.ClassEnums;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Polly;

namespace Domic.Core.Infrastructure.Concretes;

public class EventStreamBroker(
    ISerializer serializer, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment, IDateTime dateTime,
    IGlobalUniqueIdGenerator globalUniqueIdGenerator, IServiceScopeFactory serviceScopeFactory
) : IEventStreamBroker
{
    private static object _lock = new();
    
    #region Consts

    private const string GroupIdKey = "GroupId";
    private const string CountOfRetryKey = "CountOfRetry";

    #endregion
    
    public string NameOfAction { get; set; }
    public string NameOfService { get; set; }

    public async Task PublishAsync<TMessage>(string topic, TMessage message, Dictionary<string, string> headers = default,
        CancellationToken cancellationToken = default
    ) where TMessage : class
    {
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        using var producer = new ProducerBuilder<string, string>(config).Build();
        
        var retryPolicy = Policy.Handle<Exception>()
                                .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3),
                                    (exception, timeSpan, context) => throw exception
                                );

        var kafkaHeaders = new Headers();

        foreach (var header in headers)
            kafkaHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));

        kafkaHeaders.Add(GroupIdKey, Encoding.UTF8.GetBytes(""));
        kafkaHeaders.Add(CountOfRetryKey, Encoding.UTF8.GetBytes("0"));

        await retryPolicy.ExecuteAsync(() =>
            producer.ProduceAsync(topic,
                new Message<string, string> {
                    Key = message.GetType().Name, Value = serializer.Serialize(message), Headers = kafkaHeaders
                },
                cancellationToken
            )
        );
    }

    public Task PublishAsync(CancellationToken cancellationToken)
    {
        //just one worker ( Task ) in current machine ( instance ) can process outbox events => lock
        lock (_lock)
        {
            //ScopeServices Trigger
            using IServiceScope serviceScope = serviceScopeFactory.CreateScope();

            var commandUnitOfWork =
                serviceScope.ServiceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreCommandUnitOfWork;

            var redisCache = serviceScope.ServiceProvider.GetRequiredService<IInternalDistributedCache>();
            var eventCommandRepository = serviceScope.ServiceProvider.GetRequiredService<IEventCommandRepository>();
            
            try
            {
                var eventLocks = new List<string>();
                
                commandUnitOfWork.Transaction();
                
                foreach (Event targetEvent in eventCommandRepository.FindAllWithOrdering(Order.Date))
                {
                    #region DistributedLock

                    var lockEventKey = $"LockEventId-{targetEvent.Id}";
                    
                    //ReleaseLock
                    redisCache.DeleteKey(lockEventKey);
                    
                    //AcquireLock
                    var lockEventSuccessfully = redisCache.SetCacheValue(
                        new KeyValuePair<string, string>(lockEventKey, targetEvent.Id), CacheSetType.NotExists
                    );

                    #endregion

                    if (lockEventSuccessfully)
                    {
                        eventLocks.Add(lockEventKey);
                        
                        if (targetEvent.IsActive == IsActive.Active)
                        {
                            //_EventPublishHandler(channel, targetEvent);

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
                
                //ReleaseLocks
                eventLocks.ForEach(@event => redisCache.DeleteKey(@event));
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);
                
                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                    NameOfService, NameOfAction
                );
                
                // e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, this, dateTime, NameOfService, 
                //     NameOfAction
                // );

                commandUnitOfWork?.Rollback();
            }
            finally
            {
                
            }
        }

        return Task.CompletedTask;
    }

    public async Task SubscribeAsync(string topic, CancellationToken cancellationToken)
    {
        IUnitOfWork unitOfWork = null;
        
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
            int countOfRetryValue = default;
            object payloadBody = default;
            ConsumeResult<string, string> consumeResult = default;
            
            try
            {
                consumeResult = consumer.Consume(cancellationToken);
                
                var groupIdHeader = consumeResult.Message.Headers.First(h => h.Key == GroupIdKey);
                var countOfRetryHeader = consumeResult.Message.Headers.First(h => h.Key == CountOfRetryKey);
                var groupIdValue = Encoding.UTF8.GetString(groupIdHeader.GetValueBytes());
                
                countOfRetryValue = Convert.ToInt32( Encoding.UTF8.GetString(countOfRetryHeader.GetValueBytes()) );
                
                #region LoadEventStreamHandler

                var processingConditions = (
                    string.IsNullOrEmpty(groupIdValue) || (
                        groupIdValue == $"{NameOfService}-{topic}" && countOfRetryValue > 0
                    )
                );

                if (processingConditions)
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
     
                         var eventStreamHandlerType = eventStreamHandler.GetType();
     
                         payloadBody = JsonConvert.DeserializeObject(consumeResult.Message.Value, eventStreamType);
     
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
                         
                                 await (Task)afterMaxRetryHandlerMethod.Invoke(eventStreamHandler, new[] { payloadBody, cancellationToken });
                             }
                         }
                         else
                         {
                             if (eventStreamHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                             {
                                 unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as IUnitOfWork;
     
                                 unitOfWork.Transaction(transactionAttr.IsolationLevel);
     
                                 await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payloadBody, cancellationToken });
     
                                 unitOfWork.Commit();
                             }
                             else
                                 await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payloadBody, cancellationToken });
                         }
                    }
                }

                #endregion
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );

                unitOfWork?.Rollback();

                if(payloadBody != null)
                    await _RetryEventOfTopicAsync(topic, payloadBody, countOfRetryValue, cancellationToken);
            }

            _TryCommitMessageOfTopic(consumer, consumeResult);
        }
    }
    
    /*---------------------------------------------------------------*/
    
    private Type _GetTypeOfUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork))
        ) ?? domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }

    private void _TryCommitMessageOfTopic(IConsumer<string, string> consumer, ConsumeResult<string, string> consumeResult)
    {
        try
        {
            if (consumeResult is not null)
                consumer.Commit(consumeResult);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                NameOfService, NameOfAction
            );
        }
    }
    
    private async Task _RetryEventOfTopicAsync(string topic, object @event, int countOfRetry,
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
        
            using var producer = new ProducerBuilder<string, string>(config).Build();
        
            var retryPolicy = Policy.Handle<Exception>()
                                    .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(3),
                                        (exception, timeSpan, context) => throw exception
                                    );

            var kafkaHeaders = new Headers {
                { GroupIdKey, Encoding.UTF8.GetBytes( $"{NameOfService}-{topic}" ) },
                { CountOfRetryKey, Encoding.UTF8.GetBytes( $"{countOfRetry}" ) }
            };

            await retryPolicy.ExecuteAsync(() =>
                producer.ProduceAsync(topic,
                    new Message<string, string> {
                        Key = @event.GetType().Name, Value = serializer.Serialize(@event), Headers = kafkaHeaders
                    },
                    cancellationToken
                )
            );
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