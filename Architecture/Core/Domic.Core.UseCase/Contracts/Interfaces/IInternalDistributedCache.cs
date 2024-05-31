using Domic.Core.Common.ClassEnums;

namespace Domic.Core.UseCase.Contracts.Interfaces;

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
    /// <param name="cacheSetType"></param>
    /// <exception cref="NotImplementedException"></exception>
    public bool SetCacheValue(KeyValuePair<string, string> keyValue, TimeSpan time, 
        CacheSetType cacheSetType = CacheSetType.Always
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="cacheSetType"></param>
    /// <exception cref="NotImplementedException"></exception>
    public bool SetCacheValue(KeyValuePair<string, string> keyValue, CacheSetType cacheSetType = CacheSetType.Always) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="time"></param>
    /// <param name="cacheSetType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<bool> SetCacheValueAsync(KeyValuePair<string, string> keyValue, TimeSpan time, 
        CacheSetType cacheSetType = CacheSetType.Always, CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="keyValue"></param>
    /// <param name="cacheSetType"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<bool> SetCacheValueAsync(KeyValuePair<string, string> keyValue, 
        CacheSetType cacheSetType = CacheSetType.Always, CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();
    
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