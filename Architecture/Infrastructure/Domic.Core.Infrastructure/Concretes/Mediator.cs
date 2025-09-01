#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using System.Reflection;
using System.Text;
using Domic.Core.Common.ClassEnums;
using Domic.Core.Common.ClassModels;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Domic.Core.Infrastructure.Concretes;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public TResult Dispatch<TResult>(ICommand<TResult> command)
    {
        Type type        = typeof(ICommandHandler<,>);
        Type[] argTypes  = { command.GetType() , typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object commandHandler   = _serviceProvider.GetRequiredService(handlerType);
        Type commandHandlerType = commandHandler.GetType();
        
        MethodInfo commandBeforeHandlerMethod = commandHandlerType.GetMethod("BeforeHandle")
                                                ??
                                                throw new Exception("BeforeHandle function not found !");
        
        MethodInfo commandHandlerMethod = commandHandlerType.GetMethod("Handle")
                                          ??
                                          throw new Exception("Handle function not found !");
        
        MethodInfo commandAfterHandlerMethod = commandHandlerType.GetMethod("AfterHandle")
                                               ??
                                               throw new Exception("AfterHandle function not found !");

        if(commandHandlerMethod.GetCustomAttribute(typeof(WithDistributedPessimisticLockAttribute)) is WithDistributedPessimisticLockAttribute distributedLockAttr)
        {
            _BeforeHandle(commandHandler, commandBeforeHandlerMethod, command, _serviceProvider);
                    
            _Validation(commandHandler, commandHandlerType, commandHandlerMethod, command);
                
            var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
            
            _TryAcquireDistributedLock(internalDistributedCache, distributedLockAttr.Key, distributedLockAttr.Key);
                
            var result = _InvokeHandleMethod(commandHandler, commandHandlerMethod,
                commandAfterHandlerMethod, command
            );

            _TryReleaseDistributedLock(internalDistributedCache, distributedLockAttr.Key);

            return result;
        }

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithPessimisticLockAttribute)) is not null)
        {
            var lockField =
                commandHandlerType.GetField("_lock", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (lockField is not null)
            {
                var conditions = (
                    lockField.IsPrivate && 
                    lockField.FieldType == typeof(object) && 
                    lockField.GetValue(commandHandler) != null
                );
                
                if (!conditions)
                    throw new Exception("The [ _lock ] field must be private and static & return an object with value");
                    
                lock (lockField.GetValue(commandHandler))
                {
                    _BeforeHandle(commandHandler, commandBeforeHandlerMethod, command, _serviceProvider);
                    
                    _Validation(commandHandler, commandHandlerType, commandHandlerMethod, command);
                
                    return _InvokeHandleMethod(commandHandler, commandHandlerMethod, 
                        commandAfterHandlerMethod, command
                    );
                }
            }
        }
        
        _BeforeHandle(commandHandler, commandBeforeHandlerMethod, command, _serviceProvider);
        
        _Validation(commandHandler, commandHandlerType, commandHandlerMethod, command);
        
        return _InvokeHandleMethod(commandHandler, commandHandlerMethod,
            commandAfterHandlerMethod, command
        );
    }

    public void DispatchAsFireAndForget(IAsyncCommand command)
    {
        var asyncCommandBroker =
            _serviceProvider.GetRequiredService(typeof(IInternalMessageBroker)) as IInternalMessageBroker;
        
        asyncCommandBroker.Publish(command);
    }

    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        Type type        = typeof(ICommandHandler<,>);
        Type[] argTypes  = { command.GetType(), typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object commandHandler   = _serviceProvider.GetRequiredService(handlerType);
        Type commandHandlerType = commandHandler.GetType();
        
        MethodInfo commandBeforeHandlerMethod = commandHandlerType.GetMethod("BeforeHandleAsync")
                                               ??
                                               throw new Exception("BeforeHandleAsync function not found !");
        
        MethodInfo commandHandlerMethod = commandHandlerType.GetMethod("HandleAsync")
                                          ??
                                          throw new Exception("HandleAsync function not found !");
        
        MethodInfo commandAfterHandlerMethod = commandHandlerType.GetMethod("AfterHandleAsync")
                                               ??
                                               throw new Exception("AfterHandleAsync function not found !");

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithDistributedPessimisticLockAttribute)) is WithDistributedPessimisticLockAttribute distributedLockAttr)
        {
            await _BeforeHandleAsync(commandHandler, commandBeforeHandlerMethod, command,
                _serviceProvider, cancellationToken
            );
        
            await _ValidationAsync(commandHandler, commandHandlerType, commandHandlerMethod, command, cancellationToken);
        
            var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();

            await _TryAcquireDistributedLockAsync(internalDistributedCache, distributedLockAttr.Key,
                distributedLockAttr.Key, cancellationToken
            );
            
            var result =  await _InvokeHandleMethodAsync(commandHandler, commandHandlerMethod, commandAfterHandlerMethod,
                command, cancellationToken
            );

            await _TryReleaseDistributedLockAsync(internalDistributedCache, distributedLockAttr.Key, cancellationToken);

            return result;
        }

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithPessimisticLockAttribute)) is not null)
        {
            var asyncLockField =
                commandHandlerType.GetField("_asyncLock", BindingFlags.NonPublic | BindingFlags.Static);
            
            if (asyncLockField is not null)
            {
                var conditions = (
                    asyncLockField.IsPrivate &&
                    asyncLockField.FieldType == typeof(SemaphoreSlim) &&
                    asyncLockField.GetValue(commandHandler) != null
                );
                
                if (!conditions)
                    throw new Exception("The [ _asyncLock ] field must be private and static & return an [ SemaphoreSlim ] with value");

                var asyncLockValue = asyncLockField.GetValue(commandHandler) as SemaphoreSlim;
                
                await asyncLockValue.WaitAsync(cancellationToken);

                try
                {
                    await _BeforeHandleAsync(commandHandler, commandBeforeHandlerMethod, command,
                        _serviceProvider, cancellationToken
                    );
                    
                    await _ValidationAsync(commandHandler, commandHandlerType, commandHandlerMethod, command,
                        cancellationToken);

                    return await _InvokeHandleMethodAsync(commandHandler, commandHandlerMethod, 
                        commandAfterHandlerMethod, command, cancellationToken
                    );
                }
                catch (Exception e)
                {
                    asyncLockValue.Release();
                    throw;
                }
                finally
                {
                    asyncLockValue.Release();
                }
            }
        }

        await _BeforeHandleAsync(commandHandler, commandBeforeHandlerMethod, command,
            _serviceProvider, cancellationToken
        );
        
        await _ValidationAsync(commandHandler, commandHandlerType, commandHandlerMethod, command, cancellationToken);
        
        return await _InvokeHandleMethodAsync(commandHandler, commandHandlerMethod, commandAfterHandlerMethod,
            command, cancellationToken
        );
    }

    public Task DispatchAsFireAndForgetAsync(IAsyncCommand command, CancellationToken cancellationToken)
    {
        var asyncCommandBroker =
            _serviceProvider.GetRequiredService(typeof(IInternalMessageBroker)) as IInternalMessageBroker;
            
        return asyncCommandBroker.PublishAsync(command, cancellationToken);
    }
    
    public TResult Dispatch<TResult>(IQuery<TResult> query)
    {
        Type type        = typeof(IQueryHandler<,>);
        Type[] argTypes  = { query.GetType(), typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object queryHandler           = _serviceProvider.GetRequiredService(handlerType);
        Type queryHandlerType         = queryHandler.GetType();
        MethodInfo queryHandlerMethod = queryHandlerType.GetMethod("Handle") 
                                        ?? 
                                        throw new Exception("Handle function not found !");
        
        #region Validator

        //If the validation of this part is false, an exception will be thrown and the code will not be executed .

        if (queryHandlerMethod.GetCustomAttribute(typeof(WithValidationAttribute)) is not null)
        {
            Type validatorType       = typeof(IValidator<>);
            Type[] validatorArgTypes = { query.GetType() };
            Type fullValidatorType   = validatorType.MakeGenericType(validatorArgTypes);

            dynamic validator       = _serviceProvider.GetRequiredService(fullValidatorType);
            object validationResult = validator.Validate((dynamic) query);
            
            var fieldValidationResult =
                queryHandlerType.GetField("_validationResult", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (fieldValidationResult is not null)
            {
                if (
                    !fieldValidationResult.IsPrivate  ||
                    !fieldValidationResult.IsInitOnly || 
                    fieldValidationResult.FieldType != typeof(object)
                ) 
                    throw new Exception("The [ _validationResult ] field must be private and readonly & return an object");
                
                fieldValidationResult.SetValue(queryHandler, validationResult);
            }
        }

        #endregion

        #region Caching
        
        if (queryHandlerMethod.GetCustomAttribute(typeof(WithCachingAttribute)) is WithCachingAttribute cacheAttribute)
        {
            var redisCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
            
            var cachedData = redisCache.GetCacheValue(cacheAttribute.Key);
            if (cachedData is null)
            {
                var result = (TResult)queryHandlerMethod.Invoke(queryHandler, new object[]{ query });
                
                var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
                var base64 = Convert.ToBase64String(bytes);

                redisCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheAttribute.Key, base64 ) ,
                    TimeSpan.FromMinutes( cacheAttribute.Ttl )
                );

                return result;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
        }

        #endregion

        return (TResult)queryHandlerMethod.Invoke(queryHandler, new object[]{ query });
    }

    public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
    {
        Type type        = typeof(IQueryHandler<,>);
        Type[] argTypes  = { query.GetType(), typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object queryHandler           = _serviceProvider.GetRequiredService(handlerType);
        Type queryHandlerType         = queryHandler.GetType();
        MethodInfo queryHandlerMethod = queryHandlerType.GetMethod("HandleAsync")
                                        ?? 
                                        throw new Exception("HandleAsync function not found !");
        
        #region Validator

        //If the validation of this part is false, an exception will be thrown and the code will not be executed .

        if (queryHandlerMethod.GetCustomAttribute(typeof(WithValidationAttribute)) is not null)
        {
            Type validatorType       = typeof(IValidator<>);
            Type[] validatorArgTypes = { query.GetType() };
            Type fullValidatorType   = validatorType.MakeGenericType(validatorArgTypes);

            dynamic validator       = _serviceProvider.GetRequiredService(fullValidatorType);
            object validationResult = await validator.ValidateAsync((dynamic) query, (dynamic) cancellationToken);
            
            var fieldValidationResult =
                queryHandlerType.GetField("_validationResult", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (fieldValidationResult is not null)
            {
                if (
                    !fieldValidationResult.IsPrivate  ||
                    !fieldValidationResult.IsInitOnly || 
                    fieldValidationResult.FieldType != typeof(object)
                )
                    throw new Exception("The [ _validationResult ] field must be private and readonly & return an object");
                
                fieldValidationResult.SetValue(queryHandler, validationResult);
            }
        }

        #endregion
        
        #region Caching

        if (queryHandlerMethod.GetCustomAttribute(typeof(WithCachingAttribute)) is WithCachingAttribute cacheAttribute)
        {
            var redisCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
            
            var cachedData = redisCache.GetCacheValue(cacheAttribute.Key);
            
            if (cachedData is null)
            {
                var result =
                    await (Task<TResult>)queryHandlerMethod.Invoke(queryHandler, new object[]{ query , cancellationToken });

                var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
                var base64 = Convert.ToBase64String(bytes);
                
                redisCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheAttribute.Key, base64 ),
                    TimeSpan.FromMinutes( cacheAttribute.Ttl )
                );

                return result;
            }
            
            return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
        }

        #endregion
        
        object handlerResult = queryHandlerMethod.Invoke(queryHandler, new object[]{ query , cancellationToken });

        return await (Task<TResult>)handlerResult;
    }
    
    /*---------------------------------------------------------------*/
    
    private Type _GetTypeOfUnitOfWork(TransactionType transactionType)
    {
        var memoryCache = _serviceProvider.GetRequiredService<IMemoryCacheReflectionAssemblyType>();

        return transactionType == TransactionType.Command
            ? memoryCache.GetCommandUnitOfWorkType()
            : memoryCache.GetQueryUnitOfWorkType();
    }
    
    private TResult _InvokeHandleMethod<TResult>(object commandHandler, MethodInfo commandHandlerMethod,
        MethodInfo commandAfterHandlerMethod, ICommand<TResult> command
    )
    {
        #region Transaction
        
        if(commandHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
        {
            var unitOfWork = _serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionAttr.Type)) as IUnitOfWork;
            
            unitOfWork.Transaction(transactionAttr.IsolationLevel);
            
            var result = (TResult)commandHandlerMethod.Invoke(commandHandler, new object[]{ command });
            
            unitOfWork.Commit();

            _AfterHandle<TResult>(commandHandler, commandAfterHandlerMethod, command,
                _serviceProvider
            );
                    
            _CleanCache(commandHandlerMethod, _serviceProvider);

            return result;
        }

        #endregion
        
        var resultWithoutTransaction =
            (TResult)commandHandlerMethod.Invoke(commandHandler, new object[]{ command });
        
        _AfterHandle<TResult>(commandHandler, commandAfterHandlerMethod, command,
            _serviceProvider
        );
        
        _CleanCache(commandHandlerMethod, _serviceProvider);
        
        return resultWithoutTransaction;
    }

    private void _Validation<TResult>(object commandHandler, Type commandHandlerType,
        MethodInfo commandHandlerMethod, ICommand<TResult> command
    )
    {
        if (commandHandlerMethod.GetCustomAttribute(typeof(WithValidationAttribute)) is not null)
        {
            Type validatorType       = typeof(IValidator<>);
            Type[] validatorArgTypes = { command.GetType() };
            Type fullValidatorType   = validatorType.MakeGenericType(validatorArgTypes);

            dynamic validator       = _serviceProvider.GetRequiredService(fullValidatorType);
            object validationResult = validator.Validate((dynamic) command);
            
            var fieldValidationResult = 
                commandHandlerType.GetField("_validationResult", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (fieldValidationResult is not null)
            {
                if (
                    !fieldValidationResult.IsPrivate  || 
                    !fieldValidationResult.IsInitOnly ||
                    fieldValidationResult.FieldType != typeof(object)
                )
                    throw new Exception("The [ _validationResult ] field must be private and readonly & return an object");
                
                fieldValidationResult.SetValue(commandHandler, validationResult);
            }
        }
    }
    
    private async Task<TResult> _InvokeHandleMethodAsync<TResult>(object commandHandler, MethodInfo commandHandlerMethod,
        MethodInfo commandAfterHandlerMethod, ICommand<TResult> command, CancellationToken cancellationToken
    )
    {
        #region Transaction

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
        {
            var unitOfWork = _serviceProvider.GetRequiredService(_GetTypeOfUnitOfWork(transactionAttr.Type)) as IUnitOfWork;
            
            await unitOfWork.TransactionAsync(transactionAttr.IsolationLevel, cancellationToken);
            
            var result = await (Task<TResult>)commandHandlerMethod.Invoke(commandHandler, new object[]{ command , cancellationToken });
            
            await unitOfWork.CommitAsync(cancellationToken);
            
            await _AfterHandleAsync<TResult>(commandHandler, commandAfterHandlerMethod, command,
                _serviceProvider, cancellationToken
            );

            await _CleanCacheAsync(commandHandlerMethod, _serviceProvider, cancellationToken);

            return result;
        }

        #endregion
        
        var resultWithoutTransaction =
            await (Task<TResult>)commandHandlerMethod.Invoke(commandHandler, new object[]{ command , cancellationToken });
        
        await _CleanCacheAsync(commandHandlerMethod, _serviceProvider, cancellationToken);
        
        await _AfterHandleAsync<TResult>(commandHandler, commandAfterHandlerMethod, command,
            _serviceProvider, cancellationToken
        );
        
        return resultWithoutTransaction;
    }
    
    private async Task _ValidationAsync<TResult>(object commandHandler, Type commandHandlerType,
        MethodInfo commandHandlerMethod, ICommand<TResult> command, CancellationToken cancellationToken
    )
    {
        if (commandHandlerMethod.GetCustomAttribute(typeof(WithValidationAttribute)) is not null)
        {
            Type validatorType       = typeof(IValidator<>);
            Type[] validatorArgTypes = { command.GetType() };
            Type fullValidatorType   = validatorType.MakeGenericType(validatorArgTypes);

            dynamic validator       = _serviceProvider.GetRequiredService(fullValidatorType);
            object validationResult = await validator.ValidateAsync((dynamic) command, (dynamic) cancellationToken);

            var fieldValidationResult =
                commandHandlerType.GetField("_validationResult", BindingFlags.NonPublic | BindingFlags.Instance);
                    
            if (fieldValidationResult is not null)
            {
                if (
                    !fieldValidationResult.IsPrivate  || 
                    !fieldValidationResult.IsInitOnly ||
                    fieldValidationResult.FieldType != typeof(object)
                )
                    throw new Exception("The [ _validationResult ] field must be private and readonly & return an object");
                        
                fieldValidationResult.SetValue(commandHandler, validationResult);
            }
        }
    }

    private void _CleanCache(MethodInfo commandHandlerMethod, IServiceProvider serviceProvider)
    {
        if (commandHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute cacheAttribute)
        {
            try
            {
                var redisCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in cacheAttribute.Keies.Split("|"))
                    redisCache.DeleteKey(key);
            }
            catch (Exception e)
            {
                var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var dateTime = serviceProvider.GetRequiredService<IDateTime>();
                var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
                e.FileLogger(hostEnvironment, dateTime);
            
                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "CleanCacheCommand"
                );

                if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
                {
                    var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                    e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                        configuration.GetValue<string>("NameOfService"), "CleanCacheCommand"
                    );
                }
                else
                {
                    var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                    e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, 
                        externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                        "CleanCacheCommand"
                    );
                }
            }
        }
    }
    
    private async Task _CleanCacheAsync(MethodInfo commandHandlerMethod, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken
    )
    {
        if (commandHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute cacheAttribute)
        {
            try
            {
                var redisCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();

                foreach (var key in cacheAttribute.Keies.Split("|"))
                    await redisCache.DeleteKeyAsync(key, cancellationToken);
            }
            catch (Exception e)
            {
                var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
                var dateTime = serviceProvider.GetRequiredService<IDateTime>();
                var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
                //fire&forget
                e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
            
                e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "CleanCacheCommand"
                );

                if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
                {
                    var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                    //fire&forget
                    e.CentralExceptionLoggerAsync(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                        configuration.GetValue<string>("NameOfService"), "CleanCacheCommand", cancellationToken
                    );
                }
                else
                {
                    var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                    //fire&forget
                    e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, 
                        externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                        "CleanCacheCommand", cancellationToken
                    );
                }
            }
        }
    }
    
    private void _BeforeHandle<TResult>(object commandHandler, MethodInfo commandBeforeHandlerMethod,
        ICommand<TResult> command, IServiceProvider serviceProvider
    )
    {
        try
        {
            commandBeforeHandlerMethod.Invoke(commandHandler, new object[] { command });
        }
        catch (Exception e)
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var dateTime = serviceProvider.GetRequiredService<IDateTime>();
            var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                configuration.GetValue<string>("NameOfService"), "BeforeHandle"
            );

            if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
            {
                var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "BeforeHandle"
                );
            }
            else
            {
                var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, 
                    externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                    "BeforeHandle"
                );
            }
        }
    }
    
    private async Task _BeforeHandleAsync<TResult>(object commandHandler,
        MethodInfo commandBeforeHandlerMethod, ICommand<TResult> command, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            await (Task)commandBeforeHandlerMethod.Invoke(commandHandler, new object[] { command, cancellationToken });
        }
        catch (Exception e)
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var dateTime = serviceProvider.GetRequiredService<IDateTime>();
            var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                configuration.GetValue<string>("NameOfService"), "BeforeHandle"
            );

            if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
            {
                var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                //fire&forget
                e.CentralExceptionLoggerAsync(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "BeforeHandle", cancellationToken
                );
            }
            else
            {
                var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                //fire&forget
                e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, 
                    externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                    "BeforeHandle", cancellationToken
                );
            }
        }
    }
    
    private void _AfterHandle<TResult>(object commandHandler, MethodInfo commandAfterHandlerMethod,
        ICommand<TResult> command, IServiceProvider serviceProvider
    )
    {
        try
        {
            commandAfterHandlerMethod.Invoke(commandHandler, new object[] { command });
        }
        catch (Exception e)
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var dateTime = serviceProvider.GetRequiredService<IDateTime>();
            var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                configuration.GetValue<string>("NameOfService"), "AfterHandle"
            );

            if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
            {
                var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "AfterHandle"
                );
            }
            else
            {
                var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, 
                    externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                    "AfterHandle"
                );
            }
        }
    }
    
    private async Task _AfterHandleAsync<TResult>(object commandHandler,
        MethodInfo commandAfterHandlerMethod, ICommand<TResult> command, IServiceProvider serviceProvider, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            await (Task)commandAfterHandlerMethod.Invoke(commandHandler, new object[] { command, cancellationToken });
        }
        catch (Exception e)
        {
            var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
            var dateTime = serviceProvider.GetRequiredService<IDateTime>();
            var globalUniqueIdGenerator = serviceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, 
                configuration.GetValue<string>("NameOfService"), "AfterHandle"
            );

            if (configuration.GetSection("LoggerType").Get<LoggerType>().Messaging)
            {
                var externalMessageBroker = serviceProvider.GetRequiredService<IExternalMessageBroker>();
                    
                //fire&forget
                e.CentralExceptionLoggerAsync(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, 
                    configuration.GetValue<string>("NameOfService"), "AfterHandle", cancellationToken
                );
            }
            else
            {
                var externalEventStreamBroker = serviceProvider.GetRequiredService<IExternalEventStreamBroker>();
                    
                //fire&forget
                e.CentralExceptionLoggerAsStreamAsync(hostEnvironment, globalUniqueIdGenerator, 
                    externalEventStreamBroker, dateTime, configuration.GetValue<string>("NameOfService"), 
                    "AfterHandle", cancellationToken
                );
            }
        }
    }
    
    private bool _TryAcquireDistributedLock(IInternalDistributedCache distributedCache, 
        string lockKey, string lockValue
    ) => Policy.Handle<Exception>()
               .WaitAndRetry(int.MaxValue, _ => TimeSpan.FromSeconds(3))
               .Execute(() => {

                   bool acquired = false;

                   while (acquired == false)
                   {
                       acquired = distributedCache.SetCacheValue(
                           new KeyValuePair<string, string>(lockKey, lockValue),
                           TimeSpan.FromMinutes(5),
                           CacheSetType.NotExists
                       );

                       if( acquired == false )
                           Thread.Sleep( TimeSpan.FromSeconds(3) );
                   }

                   return acquired;

               });
    
    private Task<bool> _TryAcquireDistributedLockAsync(IInternalDistributedCache distributedCache, 
        string lockKey, string lockValue, CancellationToken cancellationToken
    ) => Policy.Handle<Exception>()
               .WaitAndRetryAsync(int.MaxValue, _ => TimeSpan.FromSeconds(3))
               .ExecuteAsync(async () => {
                   
                   bool acquired = false;

                   while (!cancellationToken.IsCancellationRequested && acquired == false)
                   {
                       acquired = await distributedCache.SetCacheValueAsync(
                           new KeyValuePair<string, string>(lockKey, lockValue),
                           TimeSpan.FromMinutes(5),
                           CacheSetType.NotExists,
                           cancellationToken
                       );

                       if( acquired == false )
                           await Task.Delay( TimeSpan.FromSeconds(3) );
                   }

                   return acquired;
                   
               });
    
    private bool _TryReleaseDistributedLock(IInternalDistributedCache distributedCache, string lockKey) 
        => Policy.Handle<Exception>()
                 .WaitAndRetry(int.MaxValue, _ => TimeSpan.FromSeconds(3))
                 .Execute(() => distributedCache.DeleteKey(lockKey));
    
    private Task<bool> _TryReleaseDistributedLockAsync(IInternalDistributedCache distributedCache, string lockKey,
        CancellationToken cancellationToken
    ) => Policy.Handle<Exception>()
               .WaitAndRetryAsync(int.MaxValue, _ => TimeSpan.FromSeconds(3))
               .ExecuteAsync(() => distributedCache.DeleteKeyAsync(lockKey, cancellationToken));
}