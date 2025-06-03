using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class ProducerEventStoreJob : IHostedService, IDisposable
{
    private readonly IExternalMessageBroker _externalMessageBroker;
    private readonly IConfiguration _configuration;

    private Timer _timer;

    public ProducerEventStoreJob(IExternalMessageBroker externalMessageBroker, IConfiguration configuration)
    {
        _externalMessageBroker = externalMessageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalMessageBroker.NameOfAction  = nameof(ProducerEventStoreJob);
        _externalMessageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        _timer =
            new Timer(state => _externalMessageBroker.PublishAsEventSourcingAsync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}