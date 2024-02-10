using Domic.Core.WebAPI.Exceptions;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Domic.Core.WebAPI.Filters;

public class PermissionPolicyAttribute : ActionFilterAttribute
{
    public required string Type { get; set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var username    = context.HttpContext.RequestServices.GetRequiredService<IJsonWebToken>().GetUsername(context.HttpContext.GetRowToken());
        var permissions = context.HttpContext.GetClaimsToken("Permission");
        var redisCache  = context.HttpContext.RequestServices.GetRequiredService(typeof(IRedisCache)) as IRedisCache;
        
        if (
            ( redisCache.GetCacheValue($"BlackList-{Type}")?.DeSerialize<List<string>>().Contains(username) ?? false ) ||
            !permissions.Contains(Type)
        ) 
            throw new PresentationException("شما سطح دسترسی لازم برای ورود به این قسمت را دارا نمی باشید !");
        
        await next();
    }
}