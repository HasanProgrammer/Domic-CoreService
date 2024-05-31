using Domic.Core.Common.ClassEnums;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Domic.Core.Infrastructure.Concretes;

public class ExternalDistributedCache : IExternalDistributedCache
{
    private readonly IDatabase _redisContext;
        
    public ExternalDistributedCache([FromKeyedServices("ExternalRedis")] IConnectionMultiplexer connection) 
        => _redisContext = connection.GetDatabase();

    public string GetCacheValue(string key) => _redisContext.StringGet(key);

    public async Task<string> GetCacheValueAsync(string key, CancellationToken cancellationToken) 
        => await _redisContext.StringGetAsync(key);

    public bool SetCacheValue(KeyValuePair<string, string> keyValue, TimeSpan time, 
        CacheSetType cacheSetType = CacheSetType.Always
    ) => _redisContext.StringSet(keyValue.Key, keyValue.Value, time, (When)cacheSetType);

    public bool SetCacheValue(KeyValuePair<string, string> keyValue, CacheSetType cacheSetType = CacheSetType.Always)
    {
        _redisContext.KeyPersist(keyValue.Key);
        return _redisContext.StringSet(keyValue.Key, keyValue.Value, when: (When)cacheSetType);
    }

    public Task<bool> SetCacheValueAsync(KeyValuePair<string, string> keyValue, TimeSpan time, 
        CacheSetType cacheSetType = CacheSetType.Always, CancellationToken cancellationToken = default
    ) => _redisContext.StringSetAsync(keyValue.Key, keyValue.Value, time, (When)cacheSetType);

    public async Task<bool> SetCacheValueAsync(KeyValuePair<string, string> keyValue, 
        CacheSetType cacheSetType = CacheSetType.Always, CancellationToken cancellationToken = default
    )
    {
        await _redisContext.KeyPersistAsync(keyValue.Key);
        return await _redisContext.StringSetAsync(keyValue.Key, keyValue.Value, when: (When)cacheSetType);
    }

    public bool DeleteKey(string key) => GetCacheValue(key) is not null && _redisContext.KeyDelete(key);

    public async Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken) 
        => await GetCacheValueAsync(key, cancellationToken) is not null && _redisContext.KeyDelete(key);
}