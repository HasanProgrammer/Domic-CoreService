namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TResult Handle(TQuery query) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}