namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerCommandBusHandler<in TCommand, out TResult> where TCommand : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public TResult Handle(TCommand message);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TCommand message) => throw new NotImplementedException();
}