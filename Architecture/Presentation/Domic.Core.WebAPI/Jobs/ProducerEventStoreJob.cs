using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.WebAPI.Signals;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Jobs;

public class ProducerEventStoreJob : IHostedService, IDisposable
{
    private readonly IExternalMessageBroker _externalMessageBroker;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<JobsSignal> _hubContext;
    private readonly IDateTime _dateTime;
    private readonly IExternalDistributedCache _distributedCache;

    private Timer _timer;

    public ProducerEventStoreJob(IExternalMessageBroker externalMessageBroker, IConfiguration configuration,
        IHubContext<JobsSignal> hubContext, IDateTime dateTime, IExternalDistributedCache distributedCache
    )
    {
        _externalMessageBroker = externalMessageBroker;
        _configuration = configuration;
        _hubContext = hubContext;
        _dateTime = dateTime;
        _distributedCache = distributedCache;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _externalMessageBroker.NameOfAction  = nameof(ProducerEventStoreJob);
        _externalMessageBroker.NameOfService = _configuration.GetValue<string>("NameOfService");

        var jobsCacheKey = "Jobs";
        var currentServiceJob = $"{ _externalMessageBroker.NameOfService }-{ nameof(ProducerEventStoreJob) }";
        
        var jobsCache = await _distributedCache.GetCacheValueAsync(jobsCacheKey, cancellationToken);

        if(string.IsNullOrEmpty(jobsCache))
            await _distributedCache.SetCacheValueAsync(
                new KeyValuePair<string, string>(jobsCacheKey, new string[]{ currentServiceJob }.Serialize()),
                cancellationToken: cancellationToken
            );
        else
        {
            var jobsCollection = jobsCache.DeSerialize<List<string>>();

            if(!jobsCollection.Contains(currentServiceJob))
                jobsCollection.Add(currentServiceJob);

            await _distributedCache.SetCacheValueAsync(
                new KeyValuePair<string, string>(jobsCacheKey, jobsCollection.Serialize()),
                cancellationToken: cancellationToken
            );
        }
        
        _timer =
            new Timer(state => {

                _externalMessageBroker.PublishAsEventSourcingAsync(cancellationToken);

                var nowDateTime = DateTime.UtcNow;

                _hubContext.Clients.Groups("JobsSignalGroup").SendAsync(
                    new JobsSignalDto {
                        Service = _externalMessageBroker.NameOfService,
                        Title = "ProducerEventStoreJob",
                        StartDate = _dateTime.ToPersianShortDate(nowDateTime),
                        EndDate = _dateTime.ToPersianShortDate(DateTime.UtcNow)
                    }.Serialize()
                    ,
                    cancellationToken
                );                 

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}