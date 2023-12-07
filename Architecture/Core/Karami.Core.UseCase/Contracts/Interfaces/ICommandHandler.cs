namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TResult Handle(TCommand command) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}