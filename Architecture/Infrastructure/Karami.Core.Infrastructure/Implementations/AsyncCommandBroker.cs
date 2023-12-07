using System.Reflection;
using System.Text;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Common.ClassHelpers;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Exceptions;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Attributes;
using Karami.Core.UseCase.Contracts.Abstracts;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.Exceptions;
using Karami.Core.UseCase.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Host        = Karami.Core.Common.ClassHelpers.Host;
using Environment = Karami.Core.Common.ClassConsts.Environment;

namespace Karami.Core.Domain.Implementations;

public class AsyncCommandBroker : IAsyncCommandBroker
{
    private readonly IConnection          _connection;
    private readonly IConfiguration       _configuration;
    private readonly IHostEnvironment     _hostEnvironment;
    private readonly IMessageBroker       _messageBroker;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDotrisDateTime      _dotrisDateTime;

    public AsyncCommandBroker(
        IDotrisDateTime dotrisDateTime,
        IServiceScopeFactory serviceScopeFactory, 
        IHostEnvironment hostEnvironment, 
        IConfiguration configuration,
        IMessageBroker messageBroker
    )
    {
        _dotrisDateTime      = dotrisDateTime;
        _serviceScopeFactory = serviceScopeFactory;
        _hostEnvironment     = hostEnvironment;
        _configuration       = configuration;
        _messageBroker       = messageBroker;
        
        var factory = new ConnectionFactory {
            HostName = configuration.GetInternalRabbitHostName() ,
            UserName = configuration.GetInternalRabbitUsername() ,
            Password = configuration.GetInternalRabbitPassword() ,
            Port     = configuration.GetInternalRabbitPort() 
        };
        
        _connection = factory.CreateConnection();
    }

    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    public void Publish<TCommand>(TCommand command) where TCommand : IAsyncCommand
    {
        var useCaseTypes   = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
        var commandBusType = useCaseTypes.FirstOrDefault(type => type == command.GetType());
        var messageBroker  = commandBusType.GetCustomAttribute(typeof(QueueableAttribute)) as QueueableAttribute;

        using var channel = _connection.CreateModel();

        channel.PublishMessageToDirectExchange(
            command.Serialize(), messageBroker.Exchange, messageBroker.Route,
            new Dictionary<string, object> {
                { "Command"   , commandBusType.Name      },
                { "Namespace" , commandBusType.Namespace }
            }
        );
    }

    public void Subscribe(string queue)
    {
        try
        {
            _registerAllAsyncCommandQueuesInMessageBroker();
            
            var channel = _connection.CreateModel();
            
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray());
                
                _commandOfQueueHandle(channel, args, message, NameOfService, serviceScope.ServiceProvider);
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
            e.CentralExceptionLogger(_hostEnvironment, _messageBroker, _dotrisDateTime, NameOfService, NameOfAction);
        }
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
    
    /*---------------------------------------------------------------*/
    
    private void _commandOfQueueHandle(IModel channel, BasicDeliverEventArgs args, string message, string service, 
        IServiceProvider serviceProvider
    )
    {
        Type commandBusHandlerType = null;
        ICoreUnitOfWork unitOfWork = null;
        object connectionId        = null;

        try
        {
            var nameOfCommand =
                Encoding.UTF8.GetString(
                    args.BasicProperties.Headers?.FirstOrDefault(header => header.Key.Equals("Command")).Value as byte[]
                );
            
            var nameSpaceOfCommand =
                Encoding.UTF8.GetString(
                    args.BasicProperties.Headers?.FirstOrDefault(header => header.Key.Equals("Namespace")).Value as byte[]
                );
            
            var useCaseTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
            
            var targetConsumerCommandBusHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerCommandBusHandler<,>) &&
                         i.GetGenericArguments().Any(arg => 
                             arg.Name.Equals(nameOfCommand) && arg.Namespace.Equals(nameSpaceOfCommand)
                         )
                )
            );
        
            var resultType = targetConsumerCommandBusHandlerType?.GetInterfaces()
                                                                 .Select(i => i.GetGenericArguments()[1])
                                                                 .FirstOrDefault();
            
            var commandType = targetConsumerCommandBusHandlerType?.GetInterfaces()
                                                                  .Select(i => i.GetGenericArguments()[0])
                                                                  .FirstOrDefault();

            var command  = JsonConvert.DeserializeObject(message, commandType);
            connectionId = commandType.GetProperty("ConnectionId")?.GetValue(command);
            
            var fullContractOfConsumerType =
                typeof(IConsumerCommandBusHandler<,>).MakeGenericType(commandType, resultType);
        
            var commandBusHandler = serviceProvider.GetRequiredService(fullContractOfConsumerType);
            commandBusHandlerType = commandBusHandler.GetType();
            
            var commandBusHandlerTypeMethod =
                commandBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
            
            var retryAttr =
                commandBusHandlerTypeMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

            if (_isMaxRetryMessage(args, retryAttr))
            {
                if (retryAttr.HasAfterMaxRetryHandle)
                {
                    var afterMaxRetryHandlerMethod =
                        commandBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                    
                    afterMaxRetryHandlerMethod.Invoke(commandBusHandler, new object[] { message });
                }
                
                _trySendAckMessage(channel, args); //Remove message
            }
            else
            {
                #region Validator

                //If the validation of this part is false, an exception will be thrown and the code will not be executed .

                if (commandBusHandlerTypeMethod.GetCustomAttribute(typeof(WithValidationAttribute)) is not null)
                {
                    var validatorType = useCaseTypes.FirstOrDefault(
                        type => type.GetInterfaces().Any(
                            i => i.IsGenericType &&
                                 i.GetGenericTypeDefinition() == typeof(IAsyncValidator<>) &&
                                 i.GetGenericArguments().Any(arg => 
                                     arg.Name.Equals(nameOfCommand) && arg.Namespace.Equals(nameSpaceOfCommand)
                                 )
                        )
                    );
                    
                    var validatorArgType = validatorType?.GetInterfaces()
                                                         .Select(i => i.GetGenericArguments()[0])
                                                         .FirstOrDefault();
                    
                    Type fullContractValidatorType = typeof(IAsyncValidator<>).MakeGenericType(validatorArgType);
                    
                    var validator = serviceProvider.GetRequiredService(fullContractValidatorType);
                    
                    var validatorValidateMethod =
                        validator.GetType().GetMethod("Validate") ?? throw new Exception("Validate function not found !");

                    object validationResult = validatorValidateMethod.Invoke(validator, new object[] { command });
                
                    var fieldValidationResult =
                        commandBusHandlerType.GetField("_validationResult", BindingFlags.NonPublic | BindingFlags.Instance);
                
                    if (fieldValidationResult is not null)
                    {
                        if (
                            !fieldValidationResult.IsPrivate  || 
                            !fieldValidationResult.IsInitOnly ||
                            fieldValidationResult.FieldType != typeof(object)
                        )
                            throw new Exception("The [ _validationResult ] field must be private and readonly & return an object");
                    
                        fieldValidationResult.SetValue(commandBusHandler, validationResult);
                    }
                }

                #endregion

                #region Transaction

                object resultInvokeCommand;
                
                if (commandBusHandlerTypeMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
                {
                    unitOfWork = serviceProvider.GetRequiredService(_getTypeOfCommandUnitOfWork()) as ICoreUnitOfWork;
                    
                    unitOfWork.Transaction(transactionAttr.IsolationLevel);

                    resultInvokeCommand = commandBusHandlerTypeMethod.Invoke(commandBusHandler, new object[] { command });

                    unitOfWork.Commit();
                }
                else
                    resultInvokeCommand = commandBusHandlerTypeMethod.Invoke(commandBusHandler, new object[] { command });

                #endregion
                
                _cleanCache(commandBusHandlerTypeMethod, serviceProvider);
                
                _pushSuccessNotification(connectionId?.ToString(), commandType.BaseType?.Name, resultInvokeCommand);

                _trySendAckMessage(channel, args);
            }
        }
        catch (DomainException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message ?? _configuration.GetModelValidationMessage(),
                Body    = new { }
            };
            
            _pushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload);
        }
        catch (UseCaseException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };
            
            _pushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
            
            e.CentralExceptionLogger(_hostEnvironment, _messageBroker, _dotrisDateTime, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );

            unitOfWork?.Rollback();

            _requeueMessageAsDeadLetter(channel, args);
        }
    }
    
    private void _requeueMessageAsDeadLetter(IModel channel, BasicDeliverEventArgs args)
    {
        try
        {
            channel.BasicReject(args.DeliveryTag, false);
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
    
    private Type _getTypeOfCommandUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }

    private void _registerAllAsyncCommandQueuesInMessageBroker()
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
        
        var commandBusTypes = useCaseTypes.Where(useCaseType => 
            useCaseType.BaseType?.GetInterfaces().Any(i => i == typeof(IAsyncCommand)) ?? false
        );

        using var channel = _connection.CreateModel();

        foreach (var commandBusType in commandBusTypes)
        {
            var messageBroker = commandBusType.GetCustomAttribute(typeof(QueueableAttribute)) as QueueableAttribute;

            var mainExchange    = messageBroker.Exchange;
            var retryExchange_1 = $"{messageBroker.Exchange}_Retry_1";
            var retryExchange_2 = $"{messageBroker.Exchange}_Retry_2";
            var mainQueue       = messageBroker.Queue;
            var retryQueue      = $"{messageBroker.Queue}_Retry";

            channel.DirectExchangeDeclare(mainExchange);
            channel.FanOutExchangeDeclare(retryExchange_1);
            channel.FanOutExchangeDeclare(retryExchange_2);

            channel.QueueDeclare(mainQueue, new Dictionary<string, object> {
                { "x-delayed-type"         , "fanout" },
                { "x-dead-letter-exchange" , retryExchange_1 }
            });
            channel.QueueDeclare(retryQueue, new Dictionary<string, object> {
                { "x-message-ttl"          , 5000     }, //5s
                { "x-delayed-type"         , "fanout" },
                { "x-dead-letter-exchange" , retryExchange_2 }
            });

            channel.BindQueueToDirectExchange(mainExchange, mainQueue, messageBroker.Route);
            channel.BindQueueToFanOutExchange(retryExchange_1, retryQueue);
            channel.BindQueueToFanOutExchange(retryExchange_2, mainQueue);
        }
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

    private void _pushSuccessNotification(string connectionId, string typeOfCommand, object result)
    {
        var hubConnection =
            new HubConnectionBuilder().WithUrl(
                $"{_configuration.GetNotificationServiceHubUrl(_hostEnvironment)}/notification"
            ).Build();

        try
        {
            var payload = new Payload { Body = result?.Serialize() };

            payload.Code = typeOfCommand switch 
            {
                nameof(CreateAsyncCommand) => _configuration.GetSuccessCreateStatusCode(),
                nameof(UpdateAsyncCommand) => _configuration.GetSuccessStatusCode()      ,
                nameof(DeleteAsyncCommand) => _configuration.GetSuccessStatusCode()      ,
                _ => throw new Exception("Type of command not found !")
            };

            payload.Message = typeOfCommand switch
            {
                nameof(CreateAsyncCommand) => _configuration.GetSuccessCreateMessage(),
                nameof(UpdateAsyncCommand) => _configuration.GetSuccessUpdateMessage(),
                nameof(DeleteAsyncCommand) => _configuration.GetSuccessDeleteMessage(),
                _ => throw new Exception("Type of command not found !")
            };

            var notification = new NotificationMessage { ConnectionId = connectionId, Payload = payload };

            hubConnection.StartAsync().Wait();

            hubConnection.InvokeAsync("PushAsync", notification).Wait();
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
        finally
        {
            hubConnection.DisposeAsync();
        }
    }
    
    private void _pushValidationNotification(IModel channel, BasicDeliverEventArgs args, ICoreUnitOfWork unitOfWork, 
        string connectionId, Payload payload
    )
    {
        unitOfWork?.Rollback();

        var hubConnection =
            new HubConnectionBuilder().WithUrl(_configuration.GetNotificationServiceHubUrl(_hostEnvironment)).Build();

        try
        {
            var notification = new NotificationMessage { ConnectionId = connectionId, Payload = payload };

            hubConnection.StartAsync().Wait();

            hubConnection.InvokeAsync("PushAsync", notification).Wait();
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dotrisDateTime);
        }
        finally
        {
            hubConnection.DisposeAsync();
        }

        _trySendAckMessage(channel, args);
    }
}