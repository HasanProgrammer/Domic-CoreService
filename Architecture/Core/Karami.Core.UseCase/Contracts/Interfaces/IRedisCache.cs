namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IRedisCache
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetCacheValue(string key) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<string> GetCacheValueAsync(string key, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="time"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetCacheValue(KeyValuePair<string, string> keyValue, TimeSpan time) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetCacheValue(KeyValuePair<string, string> keyValue) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="time"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task SetCacheValueAsync(KeyValuePair<string, string> keyValue, TimeSpan time,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task SetCacheValueAsync(KeyValuePair<string, string> keyValue, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool DeleteKey(string key) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}