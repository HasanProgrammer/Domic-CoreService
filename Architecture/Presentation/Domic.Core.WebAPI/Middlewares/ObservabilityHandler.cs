using Microsoft.AspNetCore.Http;

namespace Domic.Core.WebAPI.Middlewares;

public class ObservabilityHandler
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    public ObservabilityHandler(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        
        
        await _next(context);
    }
}