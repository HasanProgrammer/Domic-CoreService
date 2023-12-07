namespace Karami.Core.UseCase.Contracts.Interfaces;

/// <summary>
/// This contract just used in ( StateTrackerService )
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IConsumerMessageBusHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Handle(TMessage message) => throw new NotImplementedException();
}