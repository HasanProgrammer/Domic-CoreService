using Domic.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Domic.Core.WebAPI.Extensions;

public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="serviceName"></param>
    public static void UseCoreExceptionHandler(this IApplicationBuilder builder, string serviceName) 
        => builder.UseMiddleware<ExceptionHandler>(serviceName);
}