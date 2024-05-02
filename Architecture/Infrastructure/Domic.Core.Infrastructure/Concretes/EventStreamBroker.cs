using System.Reflection;
using Confluent.Kafka;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Concretes;

public class EventStreamBroker(
    ISerializer serializer, IServiceProvider serviceProvider, IHostEnvironment hostEnvironment, IDateTime dateTime,
    IGlobalUniqueIdGenerator globalUniqueIdGenerator
) : IEventStreamBroker
{
    public string NameOfAction { get; set; }
    public string NameOfService { get; set; }

    public async Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken)
        where TEvent : class
    {
        var config = new ProducerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password")
        };
        
        using var producer = new ProducerBuilder<string, string>(config).Build();

        await producer.ProduceAsync(topic,
            new Message<string, string> { Key = @event.GetType().Name, Value = serializer.Serialize(@event) },
            cancellationToken
        );
    }

    public async Task SubscribeAsync(string topic, CancellationToken cancellationToken)
    {
        ICoreUnitOfWork unitOfWork = null;
        
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();

        var config = new ConsumerConfig {
            BootstrapServers = Environment.GetEnvironmentVariable("E-Kafka-Host"),
            SaslUsername = Environment.GetEnvironmentVariable("E-Kafka-Username"),
            SaslPassword = Environment.GetEnvironmentVariable("E-Kafka-Password"),
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(topic);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(cancellationToken);

                #region LoadEventStreamHandler

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
                    
                    if (consumeResult.Offset > retryAttr.Count)
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
                            unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork()) as ICoreUnitOfWork;

                            unitOfWork.Transaction(transactionAttr.IsolationLevel);

                            await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payloadBody, cancellationToken });

                            unitOfWork.Commit();
                        }
                        else
                            await (Task)eventStreamHandlerMethod.Invoke(eventStreamHandler, new[] { payloadBody, cancellationToken });
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
            }
        }
        
        consumer.Close();
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
}