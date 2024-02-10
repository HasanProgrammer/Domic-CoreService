using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Handle(TEvent @event) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) => throw new NotImplementedException();
}