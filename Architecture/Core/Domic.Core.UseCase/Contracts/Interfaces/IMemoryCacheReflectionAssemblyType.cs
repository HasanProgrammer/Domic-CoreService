namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IMemoryCacheReflectionAssemblyType
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetEventTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetCommandBusTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Type GetCommandUnitOfWorkType();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public Type GetQueryUnitOfWorkType();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetEventHandlerTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetEventStreamHandlerTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetMessageStreamHandlerTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetCommandBusHandlerTypes();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public List<Type> GetCommandBusValidatorHandlerTypes();
}