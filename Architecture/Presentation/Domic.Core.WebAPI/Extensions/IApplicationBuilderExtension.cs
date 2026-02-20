using System.Diagnostics;
using Domic.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Prometheus;

namespace Domic.Core.WebAPI.Extensions;

public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void UseCoreExceptionHandler(this IApplicationBuilder builder, IConfiguration configuration) 
        => builder.UseMiddleware<ExceptionHandler>(configuration);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void UseObservibility(this IApplicationBuilder builder)
    {
        builder.UseMetricServer();
        builder.UseHttpMetrics();
        
        var requestsCounter = Metrics.CreateCounter("http_requests_total", "total http requests");

        builder.Use(async (context, next) => {
            
            requestsCounter.Inc();

            await next();
            
        });
    }
}