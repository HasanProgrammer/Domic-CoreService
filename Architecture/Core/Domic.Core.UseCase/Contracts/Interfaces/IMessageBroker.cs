using Domic.Core.UseCase.DTOs;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IMessageBroker : IDisposable
{
    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }
    
    /// <summary>
    /// This method is only used to send messages to the [ StateTracker ] service,
    /// such as error and request log messages
    /// </summary>
    /// <param name="messageBroker"></param>
    /// <typeparam name="TMessage"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish<TMessage>(MessageBrokerDto<TMessage> messageBroker)
        where TMessage : class => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="TEvent"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish() => throw new NotImplementedException();
    
    /// <summary>
    /// This method is used only in the [ StateTracker ] service
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
    /// <typeparam name="TCreateEvent"></typeparam>
    /// <typeparam name="TUpdateEvent"></typeparam>
    /// <typeparam name="TDeleteEvent"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Subscribe(string queue) => throw new NotImplementedException();
}