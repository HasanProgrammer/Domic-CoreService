using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;

namespace Karami.Core.Infrastructure.Implementations;

public class Logger : ILogger
{
    private readonly IMessageBroker           _messageBroker;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public Logger(IMessageBroker messageBroker, IGlobalUniqueIdGenerator globalUniqueIdGenerator)
    {
        _messageBroker           = messageBroker;
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
        
        var messageBrokerDto = new MessageBrokerDto<Log> {
            Message      = newLog,
            ExchangeType = Exchange.Direct,
            Exchange     = Broker.Log_Exchange,
            Route        = Broker.StateTracker_Log_Route
        };
        
        _messageBroker.Publish(messageBrokerDto);
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
        
        var messageBrokerDto = new MessageBrokerDto<Log> {
            Message      = newLog,
            ExchangeType = Exchange.Direct,
            Exchange     = Broker.Log_Exchange,
            Route        = Broker.StateTracker_Log_Route
        };
        
        return Task.Run(() => _messageBroker.Publish(messageBrokerDto), cancellationToken);
    }
}