using Domic.Core.Domain.Extensions;
using Domic.Core.WebAPI.Exceptions;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Domic.Core.WebAPI.Filters;

/// <summary>
/// This policy allows us to restrict the user's access in different ways and if the user has not yet expired ( JWT ! ),
/// we can quickly restrict the user's access at the moment, without waiting for the user's token to expire.
/// </summary>
public class BlackListPolicyAttribute : ActionFilterAttribute
{
    public string IgnoreActions { get; set; } = "";
    
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ignoreCondition = IgnoreActions.Split("|").All(
            ignore => string.IsNullOrEmpty(ignore) || 
                      string.IsNullOrEmpty(context.ActionDescriptor.DisplayName.Search(ignore))
        );
        
        if (ignoreCondition)
        {
            var externalDistributedCache =
                context.HttpContext.RequestServices.GetRequiredService<IExternalDistributedCache>();
            
            var blackListCondition =
                externalDistributedCache.GetCacheValue("BlackList-Auth")?
                                        .DeSerialize<List<string>>()
                                        .Contains( context.HttpContext.GetRowToken() ) ?? false;
            
            if (blackListCondition)
                throw new PresentationException("شما مجوز ورود به سامانه را دارا نمی باشید !");
        }

        await next();
    }
}