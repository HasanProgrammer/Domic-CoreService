using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.Infrastructure.Concretes;

public class StreamLogger : IStreamLogger
{
    private const string StateTrackerTopic = "StateTracker";
    
    private readonly IExternalEventStreamBroker       _externalEventStreamBroker;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public StreamLogger(IExternalEventStreamBroker externalEventStreamBroker, IGlobalUniqueIdGenerator globalUniqueIdGenerator)
    {
        _externalEventStreamBroker       = externalEventStreamBroker;
        _globalUniqueIdGenerator = globalUniqueIdGenerator;
    }

    public void Record(string uniqueKey, string serviceName, object item)
    {
        var newLog = new Log {
            Id          = _globalUniqueIdGenerator.GetRandom(6),
            UniqueKey   = uniqueKey   ,
            ServiceName = serviceName , 
            Item        = item
        };
        
        _externalEventStreamBroker.Publish(StateTrackerTopic, newLog);
    }

    public Task RecordAsync(string uniqueKey, string serviceName, object item,
        CancellationToken cancellationToken = default
    )
    {
        var newLog = new Log {
            Id          = _globalUniqueIdGenerator.GetRandom(6),
            UniqueKey   = uniqueKey   ,
            ServiceName = serviceName , 
            Item        = item
        };

        return _externalEventStreamBroker.PublishAsync(StateTrackerTopic, newLog, cancellationToken: cancellationToken);
    }
}