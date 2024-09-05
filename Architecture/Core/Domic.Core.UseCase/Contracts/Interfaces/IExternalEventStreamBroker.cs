namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IExternalEventStreamBroker
{
    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    #region MessageStructure
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="message"></param>
    /// <param name="headers"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish<TMessage>(string topic, TMessage message, Dictionary<string, string> headers = default) 
        where TMessage : class => throw new NotImplementedException();

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
    /// This method is used to process messages in a [Topic] in a sequential manner
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public void SubscribeMessage(string topic, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// This method processes the messages of a topic concurrently ( LongRunningTask )
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    public void SubscribeMessageAsynchronously(string topic, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// This method is used to process messages in a [Topic] in a sequential manner
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public void SubscribeRetriableMessage(string topic, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// This method processes the messages of a topic concurrently ( LongRunningTask )
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    public void SubscribeRetriableMessageAsynchronously(string topic, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    #endregion

    #region EventStructure

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
    public void Publish(CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PublishAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// This method is used to process messages in a [Topic] in a sequential manner
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public void Subscribe(string topic, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SubscribeRetriable(string topic, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// This method processes the messages of a topic concurrently ( LongRunningTask )
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    public void SubscribeAsynchronously(string topic, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SubscribeRetriableAsynchronously(string topic, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    #endregion
}