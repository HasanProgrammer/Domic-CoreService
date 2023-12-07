using System.Data;

namespace Karami.Core.Domain.Contracts.Interfaces;

public interface ICoreUnitOfWork : IDisposable
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
    /// <exception cref="NotImplementedException"></exception>
    public void Commit() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    public void Rollback() => throw new NotImplementedException();
}