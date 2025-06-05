using System.Reflection;
using System.Text;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Domic.Core.Infrastructure.Concretes;

public sealed class InternalDistributedCacheMediator : IInternalDistributedCacheMediator
{
    private readonly IServiceProvider _serviceProvider;

    public InternalDistributedCacheMediator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    
    public TResult Get<TResult>()
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IInternalDistributedCacheHandler<TResult>>();

        var cacheHandlerType   = cacheHandler.GetType();
        var cacheHandlerMethod = cacheHandlerType.GetMethod("Set") ?? throw new Exception("Set function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
        
        var cachedData = internalDistributedCache.GetCacheValue(cacheHandlerMethodAttr.Key);
        
        if (cachedData is null)
        {
            var result = (TResult)cacheHandlerMethod.Invoke(cacheHandler, null);
            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);

            if (cacheHandlerMethodAttr.Ttl is not 0)
                internalDistributedCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64) ,
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl )
                );
            else
                internalDistributedCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64) 
                );

            return result;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }

    public async Task<TResult> GetAsync<TResult>(CancellationToken cancellationToken)
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IInternalDistributedCacheHandler<TResult>>();

        var cacheHandlerType = cacheHandler.GetType();
        
        var cacheHandlerMethod =
            cacheHandlerType.GetMethod("SetAsync") ?? throw new Exception("SetAsync function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        
        var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
        
        var cachedData =
            await internalDistributedCache.GetCacheValueAsync(cacheHandlerMethodAttr.Key, cancellationToken);
        
        if (cachedData is null)
        {
            var result =
                await ( cacheHandlerMethod.Invoke(cacheHandler, new object[] { cancellationToken }) as Task<TResult> );

            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);
            
            if(cacheHandlerMethodAttr.Ttl is not 0)
                await internalDistributedCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64) ,
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl ) ,
                    cancellationToken: cancellationToken
                );
            else
                await internalDistributedCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheHandlerMethodAttr.Key, base64), 
                    cancellationToken: cancellationToken
                );

            return result;
        }
        
        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }

    public TResult Get<TResult>(string dynamicKey)
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IInternalDistributedCacheHandler<TResult>>();

        var cacheHandlerType   = cacheHandler.GetType();
        var cacheHandlerMethod = cacheHandlerType.GetMethod("Set") ?? throw new Exception("Set function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
        
        var cacheKey = $"{dynamicKey}-{cacheHandlerMethodAttr.Key}";
        
        var cachedData = internalDistributedCache.GetCacheValue(cacheKey);
        
        if (cachedData is null)
        {
            var result = (TResult)cacheHandlerMethod.Invoke(cacheHandler, null);
            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);

            if (cacheHandlerMethodAttr.Ttl is not 0)
                internalDistributedCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheKey, base64),
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl )
                );
            else
                internalDistributedCache.SetCacheValue(
                    new KeyValuePair<string, string>(cacheKey, base64)
                );

            return result;
        }

        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }

    public async Task<TResult> GetAsync<TResult>(string dynamicKey, CancellationToken cancellationToken)
    {
        object cacheHandler = _serviceProvider.GetRequiredService<IInternalDistributedCacheHandler<TResult>>();

        var cacheHandlerType = cacheHandler.GetType();
        
        var cacheHandlerMethod =
            cacheHandlerType.GetMethod("SetAsync") ?? throw new Exception("SetAsync function not found !");

        var cacheHandlerMethodAttr = cacheHandlerMethod.GetCustomAttribute(typeof(ConfigAttribute)) as ConfigAttribute
                                     ?? 
                                     throw new Exception("CachingAttribute's attribute for set function not found !");
        
        var internalDistributedCache = _serviceProvider.GetRequiredService<IInternalDistributedCache>();
        
        var cacheKey = $"{dynamicKey}-{cacheHandlerMethodAttr.Key}";
        
        var cachedData =
            await internalDistributedCache.GetCacheValueAsync(cacheKey, cancellationToken);
        
        if (cachedData is null)
        {
            var result =
                await ( cacheHandlerMethod.Invoke(cacheHandler, new object[] { cancellationToken }) as Task<TResult> );

            var bytes  = Encoding.UTF8.GetBytes(result.Serialize());
            var base64 = Convert.ToBase64String(bytes);
            
            if(cacheHandlerMethodAttr.Ttl is not 0)
                await internalDistributedCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheKey, base64) ,
                    TimeSpan.FromMinutes( cacheHandlerMethodAttr.Ttl ) ,
                    cancellationToken: cancellationToken
                );
            else
                await internalDistributedCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>(cacheKey, base64), 
                    cancellationToken: cancellationToken
                );

            return result;
        }
        
        return Encoding.UTF8.GetString(Convert.FromBase64String(cachedData)).DeSerialize<TResult>();
    }
}