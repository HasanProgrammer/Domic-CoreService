using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class ProducerEventJob : IHostedService, IDisposable
{
    private readonly IMessageBroker _messageBroker;
    private readonly IConfiguration _configuration;

    private Timer _timer;

    public ProducerEventJob(IMessageBroker messageBroker, IConfiguration configuration)
    {
        _messageBroker = messageBroker;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messageBroker.NameOfAction  = nameof(ProducerEventJob);
        _messageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");

        _timer =
            new Timer(state => Task.Run(() => _messageBroker.Publish()), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}