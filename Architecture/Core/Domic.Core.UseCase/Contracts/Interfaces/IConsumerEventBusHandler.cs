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
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TEvent @event) => throw new NotImplementedException();
}