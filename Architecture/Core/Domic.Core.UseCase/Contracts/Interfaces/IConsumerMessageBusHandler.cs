namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerMessageBusHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void BeforeHandle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task BeforeHandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void Handle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task HandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void AfterHandle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterHandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterMaxRetryHandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}