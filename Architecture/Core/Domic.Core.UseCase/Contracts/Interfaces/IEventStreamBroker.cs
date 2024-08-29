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
    public Task PublishAsync<TMessage>(string topic, TMessage message, Dictionary<string, string> headers = default,
        CancellationToken cancellationToken = default
    ) where TMessage : class => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PublishAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task SubscribeAsync(string topic, CancellationToken cancellationToken);
}