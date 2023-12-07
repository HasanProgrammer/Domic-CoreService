namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TResult Get<TResult>() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResult> GetAsync<TResult>(CancellationToken cancellationToken) => throw new NotImplementedException();
}