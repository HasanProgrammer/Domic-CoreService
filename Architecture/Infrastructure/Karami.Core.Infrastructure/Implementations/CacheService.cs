using System.Reflection;
using System.Text;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Attributes;
using Karami.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Karami.Core.Domain.Implementations;

public class CacheService : ICacheService
{
    private readonly IServiceProvider _serviceProvider;

    public CacheService(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public TResult Get<TResult>()
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IMemoryCacheSetter<TResult>>();

        var cacheHandlerType   = cacheHandler.GetType();
        var cacheHandlerMethod = cacheHandlerType.GetMethod("Set") ?? throw new Exception("Set function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        
        var redisCache = _serviceProvider.GetRequiredService<IRedisCache>();
        
        var cachedData = redisCache.GetCacheValue(cacheHandlerMethodAttr.Key);
        if (cachedData is null)
        {
            var result = (TResult)cacheHandlerMethod.Invoke(cacheHandler, null);
            
            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);

            if (cacheHandlerMethodAttr.Ttl is not 0)
                redisCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64 ) ,
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl )
                );
            else
                redisCache.SetCacheValue( new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64 ) );

            return result;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }

    public async Task<TResult> GetAsync<TResult>(CancellationToken cancellationToken)
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IMemoryCacheSetter<TResult>>();

        var cacheHandlerType   = cacheHandler.GetType();
        var cacheHandlerMethod = 
            cacheHandlerType.GetMethod("SetAsync") ?? throw new Exception("SetAsync function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        
        var redisCache = _serviceProvider.GetRequiredService<IRedisCache>();
        
        var cachedData = await redisCache.GetCacheValueAsync(cacheHandlerMethodAttr.Key, cancellationToken);
        if (cachedData is null)
        {
            var result =
                await ( cacheHandlerMethod.Invoke(cacheHandler, new object[] { cancellationToken }) as Task<TResult> );

            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);
            
            if(cacheHandlerMethodAttr.Ttl is not 0)
                await redisCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64 ) ,
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl ) ,
                    cancellationToken
                );
            else
                await redisCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64 ), cancellationToken
                );

            return result;
        }
        
        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }
}