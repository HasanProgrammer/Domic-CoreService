using Domic.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Domic.Core.WebAPI.Extensions;

public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void UseCoreExceptionHandler(this IApplicationBuilder builder, IConfiguration configuration) 
        => builder.UseMiddleware<ExceptionHandler>(configuration);
}