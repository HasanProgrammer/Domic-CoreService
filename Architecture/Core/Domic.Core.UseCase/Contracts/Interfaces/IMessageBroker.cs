using Domic.Core.UseCase.DTOs;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IMessageBroker : IDisposable, IAsyncDisposable
{
    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }

    #region MessageStructure

    /// <summary>
    ///
    /// </summary>
    /// <param name="messageBroker"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish<TMessage>(MessageBrokerDto<TMessage> messageBroker)
        where TMessage : class => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Subscribe<TMessage>(string queue) where TMessage : class
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="messageType"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Subscribe(string queue, Type messageType) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void SubscribeAsynchronously<TMessage>(string queue, CancellationToken cancellationToken) 
        where TMessage : class => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="messageType"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SubscribeAsynchronously(string queue, Type messageType, CancellationToken cancellationToken) 
        => throw new NotImplementedException();

    #endregion

    #region EventStructure
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish(CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <typeparam name="TCreateEvent"></typeparam>
    /// <typeparam name="TUpdateEvent"></typeparam>
    /// <typeparam name="TDeleteEvent"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Subscribe(string queue) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SubscribeAsynchronously(string queue, CancellationToken cancellationToken) 
        => throw new NotImplementedException();

    #endregion
}