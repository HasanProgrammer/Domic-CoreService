using System.Reflection;
using Domic.Core.Common.ClassConsts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class MemoryCacheReflectionAssemblyTypeJob(IMemoryCache memoryCache, ISerializer serializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        //all core layer types

        var domainTypes  = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        //event types in domain layer
        
        var eventTypes = domainTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IDomainEvent)) ?? false
        ).ToList();
        
        //commandBus types in useCase layer
        
        var commandBusTypes = useCaseTypes.Where(
            type => type.BaseType?.GetInterfaces().Any(i => i == typeof(IAsyncCommand)) ?? false
        ).ToList();
        
        //unit of work types in domain layer

        var commandUnitOfWorkType =
            domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork)));

        var queryUnitOfWorkType =
            domainTypes.FirstOrDefault(type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork)));
        
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
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}