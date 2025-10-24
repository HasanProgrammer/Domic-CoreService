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
        var requestsCounter = Metrics.CreateCounter("http_requests_total", "Total HTTP requests");
        var memoryGauge = Metrics.CreateGauge("app_memory_bytes", "Current memory usage in bytes");
        var cpuGauge = Metrics.CreateGauge("app_cpu_percent", "Current CPU usage percentage");
        var gcGauge = Metrics.CreateGauge("gc_total_memory_bytes", "GC total memory");

        PerformanceCounter cpuCounter = null;
        if (OperatingSystem.IsWindows())
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

        builder.UseMetricServer();
        builder.UseHttpMetrics();

        builder.Use(async (context, next) => {
            
            requestsCounter.Inc();

            memoryGauge.Set(GC.GetTotalMemory(forceFullCollection: false));

            if (cpuCounter != null)
            {
                cpuGauge.Set(cpuCounter.NextValue());
            }
            else
            {
                var process = Process.GetCurrentProcess();
                cpuGauge.Set(process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount);
            }

            gcGauge.Set(GC.GetTotalMemory(forceFullCollection: false));

            await next();
            
        });
    }
}