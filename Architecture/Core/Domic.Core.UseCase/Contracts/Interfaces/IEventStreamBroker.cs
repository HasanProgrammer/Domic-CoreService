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
    public Task PublishAsync<TEvent>(string topic, TEvent @event, CancellationToken cancellationToken) 
        where TEvent : class;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SubscribeAsync(string topic, CancellationToken cancellationToken);
}