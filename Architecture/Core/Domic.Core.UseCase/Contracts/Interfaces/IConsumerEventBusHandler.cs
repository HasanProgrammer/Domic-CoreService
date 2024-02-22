using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerEventBusHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    public void Handle(TEvent @event);

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
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TEvent @event) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandleAsync(TEvent @event, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}