namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerCommandBusHandler<in TCommand, TResult> where TCommand : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public TResult Handle(TCommand message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<TResult> HandleAsync(TCommand message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TCommand message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandleAsync(TCommand message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}