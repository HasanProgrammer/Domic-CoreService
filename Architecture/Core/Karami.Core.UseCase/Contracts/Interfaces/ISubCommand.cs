namespace Karami.Core.UseCase.Contracts.Interfaces;

/// <summary>
/// This class is for commands that are called inside a [ CommandHandler ]. In fact, [ CommandHandlers ]
/// should be broken down due to the large number of operations (multi-operational) and this type is used to
/// break down the commands of a CommandHandler . [ Command Pattern ]
/// </summary>
public interface ISubCommand<TResult>
{
    /// <summary>
    /// This function implements a work unit of the total work to be done in a single [ CommandHandler ] .
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public TResult Execute() => throw new NotImplementedException();
    
    /// <summary>
    /// This function implements a work unit of the total work to be done in a single [ CommandHandler ] .
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// This function is used to restore the changes made in the [ Execute ] function .
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Undo() => throw new NotImplementedException();
    
    /// <summary>
    /// This function is used to restore the changes made in the [ ExecuteAsync ] function .
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void UndoAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}