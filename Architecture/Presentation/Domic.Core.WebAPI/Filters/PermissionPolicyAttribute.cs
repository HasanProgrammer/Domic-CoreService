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
        var jsonWebToken = context.HttpContext.RequestServices.GetRequiredService<IJsonWebToken>();
        var externalDistributedCache = context.HttpContext.RequestServices.GetRequiredService<IExternalDistributedCache>();

        var username    = jsonWebToken.GetUsername(context.HttpContext.GetRowToken());
        var permissions = externalDistributedCache.GetCacheValue($"{username}-permissions");
        
        if (!permissions.Contains(Type))
            throw new PresentationException("شما سطح دسترسی لازم برای ورود به این قسمت را دارا نمی باشید !");
        
        await next();
    }
}