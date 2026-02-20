using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Prometheus;

namespace Domic.Core.WebAPI.Jobs;

public class ObservibilityJob : IHostedService, IDisposable
{
    private Timer _timer;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer =
            new Timer(state => {
                    
                var memoryUsageBytes = Metrics.CreateGauge("app_memory_bytes", "current memory usage in bytes");
                var cpuUsagePercentage = Metrics.CreateGauge("app_cpu_percent", "current CPU usage percentage");

                #region GC

                var gc = Metrics.CreateGauge("gc_total_memory_bytes", "gc total memory");
    
                //Gen0
                var gcGen0Count = Metrics.CreateGauge("gc_gen0_count", "gc gen0 count");
                var gcGen0Bytes = Metrics.CreateGauge("gc_gen0_memory_bytes", "gc gen0 memory bytes");
    
                //Gen2
                var gcGen2Count = Metrics.CreateGauge("gc_gen2_count", "gc gen2 count");
                var gcGen2Bytes = Metrics.CreateGauge("gc_gen2_memory_bytes", "gc gen2 memory bytes");
    
                //LOH
                var gcLohBytes = Metrics.CreateGauge("gc_loh_memory_bytes", "gc LOH memory bytes");
    
                //Allocation rate
                var allocationRateBytes = Metrics.CreateGauge("memory_allocation_bytes", "memory allocation bytes");
    
                //Heap size
                var heapSizeBytes = Metrics.CreateGauge("heap_memory_bytes", "heap memory bytes");

                #endregion

                PerformanceCounter cpuCounter = null;
                if (OperatingSystem.IsWindows())
                    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                
                #region CPU/Memory Usage Info

                memoryUsageBytes.Set(GC.GetTotalMemory(forceFullCollection: false));

                if (cpuCounter != null)
                {
                    cpuUsagePercentage.Set(cpuCounter.NextValue());
                }
                else
                {
                    var process = Process.GetCurrentProcess();
                    cpuUsagePercentage.Set(process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount);
                }

                gc.Set(GC.GetTotalMemory(forceFullCollection: false));

                #endregion

                #region Memory Usage Detail Info

                var gcGenInfo = GC.GetGCMemoryInfo().GenerationInfo;
            
                gcGen0Count.Set( GC.CollectionCount(0) );
                gcGen0Bytes.Set( gcGenInfo[0].SizeAfterBytes );
            
                gcGen2Count.Set( GC.CollectionCount(2) );
                gcGen2Bytes.Set( gcGenInfo[2].SizeAfterBytes );
            
                gcLohBytes.Set( gcGenInfo[3].SizeAfterBytes );
            
                allocationRateBytes.Set( GC.GetTotalAllocatedBytes() );
            
                heapSizeBytes.Set( GC.GetGCMemoryInfo().HeapSizeBytes );

                #endregion
                
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}