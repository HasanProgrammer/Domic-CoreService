using System.Reflection;
using System.Text;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Exceptions;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Common.ClassHelpers;
using Domic.Core.Domain.Entities;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Abstracts;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.Exceptions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Domic.Core.Infrastructure.Concretes;

public class AsyncCommandBroker : IAsyncCommandBroker
{
    private readonly IConnection              _connection;
    private readonly IConfiguration           _configuration;
    private readonly IHostEnvironment         _hostEnvironment;
    private readonly IMessageBroker           _messageBroker;
    private readonly IServiceScopeFactory     _serviceScopeFactory;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;
    private readonly IDateTime                _dateTime;

    public AsyncCommandBroker(IDateTime dateTime, IServiceScopeFactory serviceScopeFactory,
        IHostEnvironment hostEnvironment, IConfiguration configuration, IMessageBroker messageBroker, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator
    )
    {
        _dateTime                = dateTime;
        _serviceScopeFactory     = serviceScopeFactory;
        _hostEnvironment         = hostEnvironment;
        _configuration           = configuration;
        _messageBroker           = messageBroker;
        _globalUniqueIdGenerator = globalUniqueIdGenerator;
        
        var factory = new ConnectionFactory {
            HostName = configuration.GetInternalRabbitHostName() ,
            UserName = configuration.GetInternalRabbitUsername() ,
            Password = configuration.GetInternalRabbitPassword() ,
            Port     = configuration.GetInternalRabbitPort() 
        };
        
        factory.DispatchConsumersAsync = configuration.GetValue<bool>("IsInternalBrokerConsumingAsync");
        
        _connection = factory.CreateConnection();
    }

    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    public void Publish<TCommand>(TCommand command) where TCommand : IAsyncCommand
    {
        var useCaseTypes   = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
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
            _RegisterAllAsyncCommandQueuesInMessageBroker();
            
            var channel = _connection.CreateModel();
            
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (sender, args) => {
                
                //scope services trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray());
                
                _CommandOfQueueHandle(channel, args, message, NameOfService, serviceScope.ServiceProvider);
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, NameOfService, 
                NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void SubscribeAsynchronously(string queue, CancellationToken cancellationToken)
    {
        try
        {
            _RegisterAllAsyncCommandQueuesInMessageBroker();
            
            var channel = _connection.CreateModel();
            
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += async (sender, args) => {
                
                //ScopeServices trigger
                using IServiceScope serviceScope = _serviceScopeFactory.CreateScope();
                
                var message = Encoding.UTF8.GetString(args.Body.ToArray());
                
                await _CommandOfQueueHandleAsync(channel, args, message, NameOfService, serviceScope.ServiceProvider, 
                    cancellationToken
                );
                
            };

            channel.BasicConsume(queue: queue, consumer: consumer);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, NameOfService, 
                NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
                NameOfService, NameOfAction
            );
        }
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
    
    /*---------------------------------------------------------------*/
    
    private void _CommandOfQueueHandle(IModel channel, BasicDeliverEventArgs args, string message, string service, 
        IServiceProvider serviceProvider
    )
    {
        object connectionId        = null;
        IUnitOfWork unitOfWork     = null;
        Type commandBusHandlerType = null;

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
            
            var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
            
            var targetConsumerCommandBusHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerCommandBusHandler<,>) &&
                         i.GetGenericArguments().Any(arg => 
                             arg.Name.Equals(nameOfCommand) && arg.Namespace.Equals(nameSpaceOfCommand)
                         )
                )
            );

            if (targetConsumerCommandBusHandlerType is not null)
            {
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

                var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
                
                if (maxRetryInfo.result)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            commandBusHandlerType.GetMethod("AfterMaxRetryHandle") ?? throw new Exception("AfterMaxRetryHandle function not found !");
                        
                        afterMaxRetryHandlerMethod.Invoke(commandBusHandler, new object[] { message });
                    }
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

                    var transactionConfig =
                        commandBusHandlerTypeMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");
                    
                    var consumerEventCommandRepository =
                        serviceProvider.GetRequiredService<IConsumerEventCommandRepository>();
                    
                    var commandId = commandType.GetProperty("CommandId")?.GetValue(command);

                    if (commandId is null)
                        throw new Exception("The field ( commandId ) must be set!");
                    
                    var consumerEventCommand = consumerEventCommandRepository.FindById(commandId);

                    if (consumerEventCommand is null)
                    {
                        #region IdempotentConsumerPattern
                    
                        var nowDateTime = DateTime.Now;

                        consumerEventCommand = new ConsumerEvent {
                            Id = commandId.ToString(),
                            Type = nameOfCommand,
                            CountOfRetry = maxRetryInfo.countOfRetry,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventCommandRepository.Add(consumerEventCommand);

                        #endregion

                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfCommandUnitOfWork()) as IUnitOfWork;
                        
                        unitOfWork.Transaction(transactionConfig.IsolationLevel);

                        var resultInvokeCommand =
                            commandBusHandlerTypeMethod.Invoke(commandBusHandler, new object[] { command });

                        unitOfWork.Commit();
                    
                        _CleanCache(commandBusHandlerTypeMethod, serviceProvider, service, 
                            commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
                        );
                    
                        _PushSuccessNotification(connectionId?.ToString(), commandType.BaseType?.Name, 
                            resultInvokeCommand, service, 
                            commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
                        );
                    }
                    
                    #endregion
                }
            }
            
            _TrySendAckMessage(channel, args, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (DomainException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message ?? _configuration.GetModelValidationMessage(),
                Body    = new { }
            };
            
            _PushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (UseCaseException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };
            
            _PushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (Exception e)
        {
            #region Logger

            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );

            #endregion

            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
    }
    
    private async Task _CommandOfQueueHandleAsync(IModel channel, BasicDeliverEventArgs args, string message, 
        string service, IServiceProvider serviceProvider, CancellationToken cancellationToken
    )
    {
        object connectionId        = null;
        IUnitOfWork unitOfWork     = null;
        Type commandBusHandlerType = null;

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
            
            var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
            
            var targetConsumerCommandBusHandlerType = useCaseTypes.FirstOrDefault(
                type => type.GetInterfaces().Any(
                    i => i.IsGenericType &&
                         i.GetGenericTypeDefinition() == typeof(IConsumerCommandBusHandler<,>) &&
                         i.GetGenericArguments().Any(arg => 
                             arg.Name.Equals(nameOfCommand) && arg.Namespace.Equals(nameSpaceOfCommand)
                         )
                )
            );

            if (targetConsumerCommandBusHandlerType is not null)
            {
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
                    commandBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                
                var retryAttr =
                    commandBusHandlerTypeMethod.GetCustomAttribute(typeof(WithMaxRetryAttribute)) as WithMaxRetryAttribute;

                var maxRetryInfo = _IsMaxRetryMessage(args, retryAttr);
                
                if (maxRetryInfo.result)
                {
                    if (retryAttr.HasAfterMaxRetryHandle)
                    {
                        var afterMaxRetryHandlerMethod =
                            commandBusHandlerType.GetMethod("AfterMaxRetryHandleAsync") ?? throw new Exception("AfterMaxRetryHandleAsync function not found !");
                        
                        await (Task)afterMaxRetryHandlerMethod.Invoke(commandBusHandler, new object[] { message, cancellationToken });
                    }
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
                            validator.GetType().GetMethod("ValidateAsync") ?? throw new Exception("ValidateAsync function not found !");

                        object validationResult =
                            await (Task<object>)validatorValidateMethod.Invoke(validator, new[] { command, cancellationToken });
                    
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
                    
                    var transactionConfig =
                        commandBusHandlerTypeMethod.GetCustomAttribute(typeof(TransactionConfigAttribute)) as TransactionConfigAttribute;

                    if(transactionConfig is null)
                        throw new Exception("Must be used transaction config attribute!");
                    
                    var consumerEventCommandRepository =
                        serviceProvider.GetRequiredService<IConsumerEventCommandRepository>();
                    
                    var commandId = commandType.GetProperty("CommandId")?.GetValue(command);

                    if (commandId is null)
                        throw new Exception("The field ( commandId ) must be set!");
                    
                    var consumerEventCommand =
                        await consumerEventCommandRepository.FindByIdAsync(commandId, cancellationToken);

                    if (consumerEventCommand is null)
                    {
                        #region IdempotentConsumerPattern
                    
                        var nowDateTime = DateTime.Now;

                        consumerEventCommand = new ConsumerEvent {
                            Id = commandId.ToString(),
                            Type = nameOfCommand,
                            CountOfRetry = maxRetryInfo.countOfRetry,
                            CreatedAt_EnglishDate = nowDateTime,
                            CreatedAt_PersianDate = _dateTime.ToPersianShortDate(nowDateTime)
                        };

                        consumerEventCommandRepository.Add(consumerEventCommand);

                        #endregion

                        unitOfWork = serviceProvider.GetRequiredService(_GetTypeOfCommandUnitOfWork()) as IUnitOfWork;
                        
                        await unitOfWork.TransactionAsync(transactionConfig.IsolationLevel, cancellationToken);

                        var resultInvokeCommand =
                            (Task)commandBusHandlerTypeMethod.Invoke(commandBusHandler, new[] { command, cancellationToken});

                        await unitOfWork.CommitAsync(cancellationToken);
                    
                        _CleanCache(commandBusHandlerTypeMethod, serviceProvider, service,
                            commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
                        );
                    
                        _PushSuccessNotification(connectionId?.ToString(), commandType.BaseType?.Name,
                            resultInvokeCommand, service, 
                            commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
                        );
                    }
                    
                    #endregion
                }
            }
            
            _TrySendAckMessage(channel, args, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (DomainException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message ?? _configuration.GetModelValidationMessage(),
                Body    = new { }
            };
            
            _PushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (UseCaseException e)
        {
            var payload = new Payload {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };
            
            _PushValidationNotification(channel, args, unitOfWork, connectionId?.ToString(), payload, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
        catch (Exception e)
        {
            #region Logger

            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );

            #endregion

            unitOfWork?.Rollback();

            _RequeueMessageAsDeadLetter(channel, args, service, 
                commandBusHandlerType is not null ? commandBusHandlerType.Name : NameOfAction
            );
        }
    }
    
    private void _RequeueMessageAsDeadLetter(IModel channel, BasicDeliverEventArgs args, string service, string action)
    {
        try
        {
            channel.BasicReject(args.DeliveryTag, false);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                action
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
    
    private void _TrySendAckMessage(IModel channel, BasicDeliverEventArgs args, string service, string action)
    {
        try
        {
            channel.BasicAck(args.DeliveryTag, false); //delete this message from queue
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, _dateTime);

            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                action
            );
        }
    }
    
    private Type _GetTypeOfCommandUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICommandUnitOfWork))
        );
    }

    private void _RegisterAllAsyncCommandQueuesInMessageBroker()
    {
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
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
    
    private void _CleanCache(MethodInfo eventBusHandlerMethod, IServiceProvider serviceProvider, string service, 
        string action
    )
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
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                action
            );
        }
    }

    private void _PushSuccessNotification(string connectionId, string typeOfCommand, object result, string service, 
        string action
    )
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
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                action
            );
        }
        finally
        {
            hubConnection.DisposeAsync();
        }
    }
    
    private void _PushValidationNotification(IModel channel, BasicDeliverEventArgs args, IUnitOfWork unitOfWork, 
        string connectionId, Payload payload, string service, string action
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
            e.FileLogger(_hostEnvironment, _dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
                NameOfService, NameOfAction
            );
            
            e.CentralExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, service, 
                action
            );
        }
        finally
        {
            hubConnection.DisposeAsync();
        }

        _TrySendAckMessage(channel, args, service, action);
    }
}