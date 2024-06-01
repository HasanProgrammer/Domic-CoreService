using System.Data;

namespace Domic.Core.Domain.Contracts.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="isolationLevel"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Transaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="isolationLevel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task TransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, 
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Commit() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task CommitAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Rollback() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task RollbackAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}