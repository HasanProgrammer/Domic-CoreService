using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.Infrastructure.Concretes;

public class StreamLogger : IStreamLogger
{
    private const string StateTrackerTopic = "StateTracker";
    
    private readonly IEventStreamBroker       _eventStreamBroker;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public StreamLogger(IEventStreamBroker eventStreamBroker, IGlobalUniqueIdGenerator globalUniqueIdGenerator)
    {
        _eventStreamBroker       = eventStreamBroker;
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
        
        _eventStreamBroker.Publish(StateTrackerTopic, newLog);
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
        
        return Task.Run(() => _eventStreamBroker.Publish(StateTrackerTopic, newLog), cancellationToken);
    }
}