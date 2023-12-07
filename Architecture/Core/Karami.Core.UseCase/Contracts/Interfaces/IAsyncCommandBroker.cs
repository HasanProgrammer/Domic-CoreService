namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IAsyncCommandBroker : IDisposable
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
    /// <param name="queue"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Subscribe(string queue) => throw new NotImplementedException();
}