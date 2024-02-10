using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Domic.Core.WebAPI.Extensions;

public static class IEndpointRouteBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="hosRouteBuilder"></param>
    /// <param name="serviceProvider"></param>
    public static void HealthCheck(this IEndpointRouteBuilder hosRouteBuilder, IServiceProvider serviceProvider)
    {
        /*hosRouteBuilder.MapHealthChecks("/health", new HealthCheckOptions {
            ResponseWriter = async (context, report) => {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    serviceProvider.GetRequiredService<ISerializer>()
                                   .Serialize(new { Status = report.Status.ToString() })
                );
            }
        });*/

        hosRouteBuilder.MapGet("/health", context => context.Response.WriteAsync("Available"));
    }
}