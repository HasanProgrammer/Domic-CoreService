namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerCommandBusHandler<in TCommand, TResult> where TCommand : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void BeforeHandle(TCommand command)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task BeforeHandleAsync(TCommand command, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public TResult Handle(TCommand command) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterHandle(TCommand command) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterHandleAsync(TCommand command, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TCommand command) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterMaxRetryHandleAsync(TCommand command, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}