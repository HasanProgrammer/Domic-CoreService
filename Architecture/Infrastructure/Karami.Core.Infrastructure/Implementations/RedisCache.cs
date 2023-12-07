using Karami.Core.UseCase.Contracts.Interfaces;
using StackExchange.Redis;

namespace Karami.Core.Infrastructure.Implementations;

public class RedisCache : IRedisCache
{
    private readonly IDatabase _redisContext;
        
    public RedisCache(IConnectionMultiplexer connection) => _redisContext = connection.GetDatabase();

    public string GetCacheValue(string key) => _redisContext.StringGet(key);

    public async Task<string> GetCacheValueAsync(string key, CancellationToken cancellationToken) 
        => await _redisContext.StringGetAsync(key);

    public void SetCacheValue(KeyValuePair<string, string> keyValue, TimeSpan time) 
        => _redisContext.StringSet(keyValue.Key, keyValue.Value, time);

    public void SetCacheValue(KeyValuePair<string, string> keyValue)
    {
        _redisContext.KeyPersist(keyValue.Key);
        _redisContext.StringSet(keyValue.Key, keyValue.Value);
    }

    public async Task SetCacheValueAsync(KeyValuePair<string, string> keyValue, TimeSpan time, 
        CancellationToken cancellationToken
    ) => await _redisContext.StringSetAsync(keyValue.Key, keyValue.Value, time);

    public async Task SetCacheValueAsync(KeyValuePair<string, string> keyValue, CancellationToken cancellationToken)
    {
        await _redisContext.KeyPersistAsync(keyValue.Key);
        await _redisContext.StringSetAsync(keyValue.Key, keyValue.Value);
    }

    public bool DeleteKey(string key) => GetCacheValue(key) is not null && _redisContext.KeyDelete(key);

    public async Task<bool> DeleteKeyAsync(string key, CancellationToken cancellationToken) 
        => await GetCacheValueAsync(key, cancellationToken) is not null && _redisContext.KeyDelete(key);
}