using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IEventStreamBroker
{
    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task PublishAsync<TEvent>(string topic, TEvent @event, Dictionary<string, string> headers = default,
        CancellationToken cancellationToken = default
    ) where TEvent : IDomainEvent;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SubscribeAsync(string topic, CancellationToken cancellationToken);
}