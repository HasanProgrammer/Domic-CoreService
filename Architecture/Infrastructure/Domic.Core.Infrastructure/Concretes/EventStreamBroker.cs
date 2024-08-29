using System.Reflection;
using System.Text;
using Confluent.Kafka;
using Domic.Core.Domain.Contracts.Interfaces;
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
    IGlobalUniqueIdGenerator globalUniqueIdGenerator
) : IEventStreamBroker
{
    #region Consts

    private const string GroupIdKey = "GroupId";
    private const string CountOfRetryKey = "CountOfRetry";

    #endregion
    
    public string NameOfAction { get; set; }
    public string NameOfService { get; set; }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, Dictionary<string, string> headers = default,
        CancellationToken cancellationToken = default
    ) where TEvent : IDomainEvent
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

        if ( !headers.Any(h => h.Key == CountOfRetryKey || h.Key == GroupIdKey) )
        {
            kafkaHeaders.Add(GroupIdKey, Encoding.UTF8.GetBytes(""));
            kafkaHeaders.Add(CountOfRetryKey, Encoding.UTF8.GetBytes("0"));
        }

        await retryPolicy.ExecuteAsync(() =>
            producer.ProduceAsync(topic,
                new Message<string, string> {
                    Key = @event.GetType().Name, Value = serializer.Serialize(@event), Headers = kafkaHeaders
                },
                cancellationToken
            )
        );
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
            int countOfRetryValue;
            object @event;
            
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

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
     
                         var payloadBody = JsonConvert.DeserializeObject(consumeResult.Message.Value, eventStreamType);
     
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

                consumer.Commit(consumeResult);
            }
            catch (Exception e)
            {
                e.FileLogger(hostEnvironment, dateTime);

                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                    NameOfService, NameOfAction
                );

                unitOfWork?.Rollback();

                //_RetryMessageOfTopicAsync(topic, @event, countOfRetryValue);
            }
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
    
    private async Task _RetryMessageOfTopicAsync(string topic, object @event, int countOfRetry,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var headers = new Dictionary<string, string> {
                { GroupIdKey, $"{NameOfService}-{topic}" },
                { CountOfRetryKey, $"{countOfRetry}" }
            };
            
            //await PublishAsync(topic, @event, headers, cancellationToken);
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