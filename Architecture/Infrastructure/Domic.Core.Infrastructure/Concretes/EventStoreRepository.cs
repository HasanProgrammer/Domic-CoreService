using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.Persistence.Contexts;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Action = Domic.Core.Common.ClassConsts.Action;

namespace Domic.Core.Infrastructure.Concretes;

public class EventStoreRepository(EventStoreContext context, IConfiguration configuration,
    IGlobalUniqueIdGenerator globalUniqueIdGenerator, 
    IMemoryCacheReflectionAssemblyType memoryCacheReflectionAssemblyType
) : IEventStoreRepository
{
    public void Append(IEnumerable<IDomainEvent> events)
    {
        List<EventStore<string>> eventModels = [];
        
        foreach (var @event in events)
        {
            var createDomainEvent = @event as CreateDomainEvent<string>;
            var updateDomainEvent = @event as UpdateDomainEvent<string>;
        
            var newModel = new EventStore<string> {
                Id = globalUniqueIdGenerator.GetRandom(6),
                AggregateId = createDomainEvent?.Id ?? updateDomainEvent?.Id, //EventId = EntityId
                NameOfService = configuration.GetValue<string>("NameOfService"),
                NameOfEvent = @event.GetType().Name,
                Action = createDomainEvent is not null ? Action.Create : Action.Update,
                Payload = @event.Serialize(),
                CreatedAt = createDomainEvent?.CreatedAt_EnglishDate ?? updateDomainEvent.UpdatedAt_EnglishDate,
                CreatedBy = createDomainEvent?.CreatedBy ?? updateDomainEvent?.UpdatedBy,
                CreatedRole = createDomainEvent?.CreatedRole ?? updateDomainEvent?.UpdatedRole
            };
            
            eventModels.Add(newModel);
        }

        context.EventStores.AddRange(eventModels);
    }

    public async Task AppendAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
    {
        List<EventStore<string>> eventModels = [];
        
        foreach (var @event in events)
        {
            var createDomainEvent = @event as CreateDomainEvent<string>;
            var updateDomainEvent = @event as UpdateDomainEvent<string>;
        
            var newModel = new EventStore<string> {
                Id = globalUniqueIdGenerator.GetRandom(6),
                AggregateId = createDomainEvent?.Id ?? updateDomainEvent?.Id, //EventId = EntityId
                NameOfService = configuration.GetValue<string>("NameOfService"),
                NameOfEvent = @event.GetType().Name,
                Action = createDomainEvent is not null ? Action.Create : Action.Update,
                Payload = @event.Serialize(),
                CreatedAt = createDomainEvent?.CreatedAt_EnglishDate ?? updateDomainEvent.UpdatedAt_EnglishDate,
                CreatedBy = createDomainEvent?.CreatedBy ?? updateDomainEvent?.UpdatedBy,
                CreatedRole = createDomainEvent?.CreatedRole ?? updateDomainEvent?.UpdatedRole
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
                                                  .OrderBy(@event => @event.CreatedAt)
                                                  .Select(@event => new { Type = @event.NameOfEvent, @event.Payload })
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
                                     .OrderBy(@event => @event.CreatedAt)
                                     .Select(@event => new { Type = @event.NameOfEvent, @event.Payload })
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