using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class ProducerEventStreamJob : IHostedService, IDisposable
{
    private readonly IExternalEventStreamBroker _externalEventStreamBroker;
    private readonly IConfiguration _configuration;

    private Timer _timer;

    public ProducerEventStreamJob(IExternalEventStreamBroker externalEventStreamBroker, IConfiguration configuration)
    {
        _externalEventStreamBroker = externalEventStreamBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalEventStreamBroker.NameOfAction  = nameof(ProducerEventStreamJob);
        _externalEventStreamBroker.NameOfService = _configuration.GetValue<string>("NameOfService");
        
        #if false
        
        //sync
        _timer =
            new Timer(state => Task.Run(() => _eventStreamBroker.Publish(cancellationToken), cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

        #endif

        //async
        _timer =
            new Timer(state => _externalEventStreamBroker.PublishAsync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}