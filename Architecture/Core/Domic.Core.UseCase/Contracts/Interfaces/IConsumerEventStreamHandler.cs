namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerEventStreamHandler<in TEvent> where TEvent : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterMaxRetryHandleAsync(TEvent @event, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}