using System.Reflection;
using Domic.Core.Common.ClassConsts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Domic.Core.Infrastructure.Concretes;

public sealed class MemoryCacheReflectionAssemblyType : IMemoryCacheReflectionAssemblyType
{
    private readonly IMemoryCache _memoryCache;
    private readonly ISerializer  _serializer;

    public MemoryCacheReflectionAssemblyType(IMemoryCache memoryCache, ISerializer serializer)
    {
        _serializer  = serializer;
        _memoryCache = memoryCache;
        
        //all core layer types

        var domainTypes  = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        //event types in domain layer
        
        var eventTypes = domainTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false
        ).ToList();
        
        Log.Information($"\n eventTypes: { eventTypes } \n");
        
        //commandBus types in useCase layer
        
        var commandBusTypes = useCaseTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IAsyncCommand)) ?? false
        ).ToList();
        
        Log.Information($"\n commandBusTypes: { commandBusTypes } \n");
        
        //unit of work types in domain layer

        var commandUnitOfWorkType =
            domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork)));

        var queryUnitOfWorkType =
            domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork)));
        
        Log.Information($"\n commandUnitOfWorkType: { commandUnitOfWorkType } \n");
        Log.Information($"\n queryUnitOfWorkType: { queryUnitOfWorkType } \n");
        
        //event & message and commandBus handler types in useCase layer
        
        var eventHandlerTypes = useCaseTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerEventBusHandler<>)
            )
        ).ToList();
        
        var eventStreamHandlerTypes = useCaseTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>)
            )
        ).ToList();
        
        var messageStreamHandlerTypes = useCaseTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>)
            )
        ).ToList();
        
        var commandBusHandlerTypes = useCaseTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerCommandBusHandler<,>)
            )
        ).ToList();
        
        var commandBusValidatorHandlerTypes = useCaseTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncValidator<>)
            )
        ).ToList();
        
        Log.Information($"\n eventHandlerTypes: { eventHandlerTypes } \n");
        Log.Information($"\n eventStreamHandlerTypes: { eventStreamHandlerTypes } \n");
        Log.Information($"\n messageStreamHandlerTypes: { messageStreamHandlerTypes } \n");
        Log.Information($"\n commandBusHandlerTypes: { commandBusHandlerTypes } \n");
        Log.Information($"\n commandBusValidatorHandlerTypes: { commandBusValidatorHandlerTypes } \n");
        
        #region MemoryCacheSetter

        if(eventTypes.Any())
            memoryCache.Set(Reflection.DomainEvent, serializer.Serialize(eventTypes));
        
        if(commandUnitOfWorkType is not null)
            memoryCache.Set(Reflection.DomainCommandUnitOfWork, serializer.Serialize(commandUnitOfWorkType));
        
        if(queryUnitOfWorkType is not null)
            memoryCache.Set(Reflection.DomainQueryUnitOfWork, serializer.Serialize(queryUnitOfWorkType));
        
        if(eventHandlerTypes.Any())
            memoryCache.Set(Reflection.UseCaseEventHandler, serializer.Serialize(eventHandlerTypes));

        if(eventStreamHandlerTypes.Any())
            memoryCache.Set(Reflection.UseCaseEventStreamHandler, serializer.Serialize(eventStreamHandlerTypes));
        
        if(messageStreamHandlerTypes.Any())
            memoryCache.Set(Reflection.UseCaseMessageStreamHandler, serializer.Serialize(messageStreamHandlerTypes));
        
        if(commandBusTypes.Any())
            memoryCache.Set(Reflection.UseCaseCommandBus, serializer.Serialize(commandBusTypes));
        
        if(commandBusHandlerTypes.Any())
            memoryCache.Set(Reflection.UseCaseCommandBusHandler, serializer.Serialize(commandBusHandlerTypes));
        
        if(commandBusValidatorHandlerTypes.Any())
            memoryCache.Set(Reflection.UseCaseCommandBusValidatorHandler, serializer.Serialize(commandBusValidatorHandlerTypes));
        
        #endregion
    }
    
    public List<Type> GetEventTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.DomainEvent) as string );

    public List<Type> GetCommandBusTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseCommandBus) as string );

    public Type GetCommandUnitOfWorkType() 
        => _serializer.DeSerialize<Type>( _memoryCache.Get(Reflection.DomainCommandUnitOfWork) as string );

    public Type GetQueryUnitOfWorkType() 
        => _serializer.DeSerialize<Type>( _memoryCache.Get(Reflection.DomainQueryUnitOfWork) as string );

    public List<Type> GetEventHandlerTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseEventHandler) as string );

    public List<Type> GetEventStreamHandlerTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseEventStreamHandler) as string );

    public List<Type> GetMessageStreamHandlerTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseMessageStreamHandler) as string );

    public List<Type> GetCommandBusHandlerTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseCommandBusHandler) as string );

    public List<Type> GetCommandBusValidatorHandlerTypes() 
        => _serializer.DeSerialize<List<Type>>( _memoryCache.Get(Reflection.UseCaseCommandBusValidatorHandler) as string );
}