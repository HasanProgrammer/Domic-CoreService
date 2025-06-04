#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class RefreshSecretKeyJob(
    IExternalDistributedCache distributedCache, IHostEnvironment hostEnvironment, IDateTime dateTime, 
    IGlobalUniqueIdGenerator globalUniqueIdGenerator, IConfiguration configuration, IExternalMessageBroker messageBroker
) : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer =
            new Timer(state => {

                    try
                    {
                        distributedCache.SetCacheValue(
                            new KeyValuePair<string, string>("SecretKey", Guid.NewGuid().ToString())
                        );
                    }
                    catch (Exception e)
                    {
                        //fire&forget
                        e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken: cancellationToken);

                        e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime,
                            configuration.GetValue<string>("NameOfService"), nameof(RefreshSecretKeyJob)
                        );

                        //fire&forget
                        e.CentralExceptionLoggerAsync(hostEnvironment, globalUniqueIdGenerator, messageBroker, dateTime,
                            configuration.GetValue<string>("NameOfService"), nameof(RefreshSecretKeyJob),
                            cancellationToken: cancellationToken
                        );
                    }
                    
                },
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