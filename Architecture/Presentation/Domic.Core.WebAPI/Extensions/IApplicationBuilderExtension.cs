using Domic.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Domic.Core.WebAPI.Extensions;

public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void UseCoreExceptionHandler(this IApplicationBuilder builder) 
        => builder.UseMiddleware<ExceptionHandler>();
}