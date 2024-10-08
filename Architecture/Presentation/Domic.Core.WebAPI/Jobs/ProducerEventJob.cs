using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class ProducerEventJob : IHostedService, IDisposable
{
    private readonly IExternalMessageBroker _externalMessageBroker;
    private readonly IConfiguration _configuration;

    private Timer _timer;

    public ProducerEventJob(IExternalMessageBroker externalMessageBroker, IConfiguration configuration)
    {
        _externalMessageBroker = externalMessageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _externalMessageBroker.NameOfAction  = nameof(ProducerEventJob);
        _externalMessageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");

        #if false
        
        _timer =
            new Timer(state => Task.Run(() => _messageBroker.Publish(cancellationToken), cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        #endif
        
        _timer =
            new Timer(state => _externalMessageBroker.PublishAsync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}