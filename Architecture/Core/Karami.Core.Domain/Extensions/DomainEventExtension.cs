using System.Collections.ObjectModel;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;

namespace Karami.Core.Domain.Extensions;

public static class DomainEventExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="domainEvents"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="serializer"></param>
    /// <param name="service"></param>
    /// <param name="table"></param>
    /// <param name="action"></param>
    /// <param name="username"></param>
    /// <returns></returns>
    public static IEnumerable<Event> ToEntityOfEvent(this ReadOnlyCollection<IDomainEvent> domainEvents, 
        IDotrisDateTime dotrisDateTime, ISerializer serializer, string service, string table, string action, string username
    )
    {
        var nowDateTime        = DateTime.Now;
        var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
        
        return domainEvents.Select(
            @event => new Event
            {
                Id        = Guid.NewGuid().ToString()      ,
                Type      = @event.GetType().Name          ,
                Service   = service                        ,
                Payload   = serializer.Serialize(@event)   ,
                Table     = table                          ,
                Action    = action                         ,
                User      = username                       ,
                CreatedAt_EnglishDate = nowDateTime        ,
                CreatedAt_PersianDate = nowPersianDateTime ,
                UpdatedAt_EnglishDate = nowDateTime        ,
                UpdatedAt_PersianDate = nowPersianDateTime
            }
        );
    }
}