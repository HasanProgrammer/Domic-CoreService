using Domic.Core.Common.ClassConsts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Domic.Core.Infrastructure.Concretes;

public class MemoryCacheReflectionAssemblyType(IMemoryCache memoryCache, ISerializer serializer) : IMemoryCacheReflectionAssemblyType
{
    public List<Type> GetEventTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.DomainEvent) as string );

    public List<Type> GetCommandBusTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseCommandBus) as string );

    public Type GetCommandUnitOfWorkType() 
        => serializer.DeSerialize<Type>( memoryCache.Get(Reflection.DomainCommandUnitOfWork) as string );

    public Type GetQueryUnitOfWorkType() 
        => serializer.DeSerialize<Type>( memoryCache.Get(Reflection.DomainQueryUnitOfWork) as string );

    public List<Type> GetEventHandlerTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseEventHandler) as string );

    public List<Type> GetEventStreamHandlerTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseEventStreamHandler) as string );

    public List<Type> GetMessageStreamHandlerTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseMessageStreamHandler) as string );

    public List<Type> GetCommandBusHandlerTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseCommandBusHandler) as string );

    public List<Type> GetCommandBusValidatorHandlerTypes() 
        => serializer.DeSerialize<List<Type>>( memoryCache.Get(Reflection.UseCaseCommandBusValidatorHandler) as string );
}