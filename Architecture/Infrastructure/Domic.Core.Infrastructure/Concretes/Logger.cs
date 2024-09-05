using Domic.Core.Domain.Constants;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;

namespace Domic.Core.Infrastructure.Concretes;

public class Logger : ILogger
{
    private readonly IExternalMessageBroker           _externalMessageBroker;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public Logger(IExternalMessageBroker externalMessageBroker, IGlobalUniqueIdGenerator globalUniqueIdGenerator)
    {
        _externalMessageBroker           = externalMessageBroker;
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
        
        _externalMessageBroker.Publish(messageBrokerDto);
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
        
        return Task.Run(() => _externalMessageBroker.Publish(messageBrokerDto), cancellationToken);
    }
}