namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IInternalMessageBroker : IDisposable
{
    public string NameOfAction  { get; set; }
    public string NameOfService { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <exception cref="NotImplementedException"></exception>
    public void Publish<TCommand>(TCommand command)
        where TCommand : IAsyncCommand => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PublishAsync<TCommand>(TCommand command, CancellationToken cancellationToken)
        where TCommand : IAsyncCommand => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
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
}