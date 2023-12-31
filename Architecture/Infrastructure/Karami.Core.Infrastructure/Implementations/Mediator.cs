using System.Reflection;
using System.Text;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Attributes;
using Karami.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Environment = Karami.Core.Common.ClassConsts.Environment;

namespace Karami.Core.Domain.Implementations;

//DI
public partial class Mediator : IMediator
{
    private static object _lock = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _hostEnvironment;

    public Mediator(IServiceProvider serviceProvider, IHostEnvironment hostEnvironment)
    {
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
    }
}

//Command & Query
public partial class Mediator
{
    public TResult Dispatch<TResult>(ICommand<TResult> command)
    {
        Type type        = typeof(ICommandHandler<,>);
        Type[] argTypes  = { command.GetType() , typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object commandHandler           = _serviceProvider.GetRequiredService(handlerType);
        Type commandHandlerType         = commandHandler.GetType();
        MethodInfo commandHandlerMethod = commandHandlerType.GetMethod("Handle") 
                                          ??
                                          throw new Exception("Handle function not found !");

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithPessimisticConcurrencyAttribute)) is not null)
        {
            lock (_lock)
            {
                _validation(commandHandler, commandHandlerType, commandHandlerMethod, command);
                
                return _invokeHandleMethod(commandHandler, commandHandlerMethod, command);
            }
        }
        
        _validation(commandHandler, commandHandlerType, commandHandlerMethod, command);
        
        return _invokeHandleMethod(commandHandler, commandHandlerMethod, command);
    }

    public void DispatchAsFireAndForget(IAsyncCommand command)
    {
        var asyncCommandBroker =
            _serviceProvider.GetRequiredService(typeof(IAsyncCommandBroker)) as IAsyncCommandBroker;
        
        asyncCommandBroker.Publish(command);
    }

    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        Type type        = typeof(ICommandHandler<,>);
        Type[] argTypes  = { command.GetType(), typeof(TResult) };
        Type handlerType = type.MakeGenericType(argTypes);
        
        object commandHandler           = _serviceProvider.GetRequiredService(handlerType);
        Type commandHandlerType         = commandHandler.GetType();
        MethodInfo commandHandlerMethod = commandHandlerType.GetMethod("HandleAsync")
                                          ??
                                          throw new Exception("HandleAsync function not found !");

        #region Validator

        //If the validation of this part is false, an exception will be thrown and the code will not be executed .
        
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

        #endregion

        #region Transaction

        if (commandHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
        {
            var unitOfWork = _serviceProvider.GetRequiredService(_getTypeOfUnitOfWork()) as ICoreCommandUnitOfWork;
            
            unitOfWork.Transaction(transactionAttr.IsolationLevel);
            
            var result = await (Task<TResult>)commandHandlerMethod.Invoke(commandHandler, new object[]{ command , cancellationToken });
            
            unitOfWork.Commit();

            _cleanCache(commandHandlerMethod);

            return result;
        }

        #endregion

        var resultWithoutTransaction =
            await (Task<TResult>)commandHandlerMethod.Invoke(commandHandler, new object[]{ command , cancellationToken });

        _cleanCache(commandHandlerMethod);
        
        return resultWithoutTransaction;
    }

    public async Task DispatchAsFireAndForgetAsync(IAsyncCommand command, CancellationToken cancellationToken)
    {
        await Task.Run(() => {
            
            var asyncCommandBroker =
                _serviceProvider.GetRequiredService(typeof(IAsyncCommandBroker)) as IAsyncCommandBroker;
        
            asyncCommandBroker.Publish(command);
            
        }, cancellationToken);
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
            var redisCache = _serviceProvider.GetRequiredService<IRedisCache>();
            
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
            var redisCache = _serviceProvider.GetRequiredService<IRedisCache>();
            
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
}

//Private functions
public partial class Mediator
{
    private Type _getTypeOfUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
        );
    }
    
    private TResult _invokeHandleMethod<TResult>(object commandHandler, MethodInfo commandHandlerMethod,
        ICommand<TResult> command
    )
    {
        #region Transaction
        
        if(commandHandlerMethod.GetCustomAttribute(typeof(WithTransactionAttribute)) is WithTransactionAttribute transactionAttr)
        {
            var unitOfWork = _serviceProvider.GetRequiredService(_getTypeOfUnitOfWork()) as ICoreCommandUnitOfWork;
            
            unitOfWork.Transaction(transactionAttr.IsolationLevel);
            
            var result = (TResult)commandHandlerMethod.Invoke(commandHandler, new object[]{ command });
            
            unitOfWork.Commit();
                    
            _cleanCache(commandHandlerMethod);

            return result;
        }

        #endregion
        
        var resultWithoutTransaction =
            (TResult)commandHandlerMethod.Invoke(commandHandler, new object[]{ command });
            
        _cleanCache(commandHandlerMethod);
        
        return resultWithoutTransaction;
    }

    private void _validation<TResult>(object commandHandler, Type commandHandlerType,
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

    private void _cleanCache(MethodInfo commandHandlerMethod)
    {
        if (commandHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute cacheAttribute)
        {
            var redisCache = _serviceProvider.GetRequiredService<IRedisCache>();

            foreach (var key in cacheAttribute.Keies.Split("|")) 
                redisCache.DeleteKey(key);
        }
    }
}