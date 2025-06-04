using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class RefreshSecretKeyJob(IExternalDistributedCache distributedCache) : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer =
            new Timer(state =>
                distributedCache.SetCacheValue(
                    new KeyValuePair<string, string>("SecretKey", Guid.NewGuid().ToString())
                ),
                null,
                TimeSpan.Zero, 
                TimeSpan.FromMinutes(30)
            );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset

        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}