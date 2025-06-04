using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.Persistence.Contexts;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Action = Domic.Core.Common.ClassConsts.Action;

namespace Domic.Core.Infrastructure.Concretes;

public class EventStoreRepository(
    EventStoreContext context, IConfiguration configuration,
    IGlobalUniqueIdGenerator globalUniqueIdGenerator, 
    IMemoryCacheReflectionAssemblyType memoryCacheReflectionAssemblyType
) : IEventStoreRepository
{
    public List<Event> FindAll()
        => context.EventStores.AsNoTracking()
                              .Where(es => 
                                  es.IsActive == IsActive.Active && 
                                  es.CreatedAt_EnglishDate <= DateTime.Now &&
                                  es.CreatedAt_EnglishDate >= DateTime.Now.AddDays(-1)
                              )
                              .OrderBy(es => es.CreatedAt_EnglishDate)
                              .ToList();

    public Task<List<Event>> FindAllAsync(CancellationToken cancellationToken)
        => context.EventStores.AsNoTracking()
                              .Where(es =>
                                  es.IsActive == IsActive.Active && 
                                  es.CreatedAt_EnglishDate <= DateTime.Now &&
                                  es.CreatedAt_EnglishDate >= DateTime.Now.AddDays(-1)
                              )
                              .OrderBy(es => es.CreatedAt_EnglishDate)
                              .ToListAsync(cancellationToken);

    public void Change(Event @event) => context.EventStores.Update(@event);

    public Task ChangeAsync(Event @event, CancellationToken cancellationToken)
    {
        context.EventStores.Update(@event);
        
        return Task.CompletedTask;
    }

    public void Append(IEnumerable<IDomainEvent> events)
    {
        List<Event> eventModels = [];
        
        foreach (var @event in events)
        {
            var createDomainEvent = @event as CreateDomainEvent<string>;
            var updateDomainEvent = @event as UpdateDomainEvent<string>;
        
            var newModel = new Event {
                Id = globalUniqueIdGenerator.GetRandom(6),
                AggregateId = createDomainEvent?.Id ?? updateDomainEvent?.Id, //EventId = EntityId
                Service = configuration.GetValue<string>("NameOfService"),
                Action = createDomainEvent is not null ? Action.Create : Action.Update,
                Type = @event.GetType().Name,
                Payload = @event.Serialize(),
                CreatedAt_EnglishDate = createDomainEvent?.CreatedAt_EnglishDate ?? updateDomainEvent.UpdatedAt_EnglishDate,
                CreatedAt_PersianDate = createDomainEvent?.CreatedAt_PersianDate ?? updateDomainEvent.UpdatedAt_PersianDate,
            };
            
            eventModels.Add(newModel);
        }

        context.EventStores.AddRange(eventModels);
    }

    public async Task AppendAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        List<Event> eventModels = [];
        
        foreach (var @event in events)
        {
            var createDomainEvent = @event as CreateDomainEvent<string>;
            var updateDomainEvent = @event as UpdateDomainEvent<string>;
        
            var newModel = new Event {
                Id = globalUniqueIdGenerator.GetRandom(6),
                AggregateId = createDomainEvent?.Id ?? updateDomainEvent?.Id, //EventId = EntityId
                Service = configuration.GetValue<string>("NameOfService"),
                Action = createDomainEvent is not null ? Action.Create : Action.Update,
                Type = @event.GetType().Name,
                Payload = @event.Serialize(),
                CreatedAt_EnglishDate = createDomainEvent?.CreatedAt_EnglishDate ?? updateDomainEvent.UpdatedAt_EnglishDate,
                CreatedAt_PersianDate = createDomainEvent?.CreatedAt_PersianDate ?? updateDomainEvent.UpdatedAt_PersianDate,
            };
            
            eventModels.Add(newModel);
        }

        await context.EventStores.AddRangeAsync(eventModels, cancellationToken);
    }

    public IEnumerable<IDomainEvent> Load<TAggregateIdentity>(TAggregateIdentity identity)
    {
        var eventTypes = memoryCacheReflectionAssemblyType.GetEventTypes();
        
        var serializedEvents = context.EventStores.AsNoTracking()
                                                  .Where(@event => @event.AggregateId == identity as string)
                                                  .OrderBy(@event => @event.CreatedAt_EnglishDate)
                                                  .Select(@event => new { @event.Type, @event.Payload })
                                                  .ToList();

        return serializedEvents.Select(@event =>
            (IDomainEvent)@event.Payload.DeSerialize(eventTypes.FirstOrDefault(et => et.Name == @event.Type))
        );
    }

    public async Task<IEnumerable<IDomainEvent>> LoadAsync<TAggregateIdentity>(TAggregateIdentity identity, 
        CancellationToken cancellationToken
    )
    {
        var eventTypes = memoryCacheReflectionAssemblyType.GetEventTypes();
        
        var serializedEvents =
            await context.EventStores.AsNoTracking()
                                     .Where(@event => @event.AggregateId == identity as string)
                                     .OrderBy(@event => @event.CreatedAt_EnglishDate)
                                     .Select(@event => new { @event.Type, @event.Payload })
                                     .ToListAsync(cancellationToken);

        return serializedEvents.Select(@event =>
            (IDomainEvent)@event.Payload.DeSerialize(eventTypes.FirstOrDefault(et => et.Name == @event.Type))
        );
    }

    public TEntity AssembleEntity<TEntity, TAggregateIdentity>(TAggregateIdentity identity) where TEntity 
        : Entity<TAggregateIdentity>, new()
    {
        var events = Load(identity);
        
        var entity = new TEntity();

        var targetTypeOfEntity = entity.GetType();

        var applyAllMethod = targetTypeOfEntity.GetMethod("ApplyAll");
        
        applyAllMethod.Invoke(targetTypeOfEntity, new object[] { events });

        return entity;
    }

    public async Task<TEntity> AssembleEntityAsync<TEntity, TAggregateIdentity>(TAggregateIdentity identity, 
        CancellationToken cancellationToken
    ) where TEntity : Entity<TAggregateIdentity>, new()
    {
        var events = await LoadAsync(identity, cancellationToken);

        var entity = new TEntity();

        var targetTypeOfEntity = entity.GetType();

        var applyAllMethod = targetTypeOfEntity.GetMethod("ApplyAll");
        
        applyAllMethod.Invoke(targetTypeOfEntity, new object[] { events });

        return entity;
    }
}