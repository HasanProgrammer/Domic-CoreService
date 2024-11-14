using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

using Action = Domic.Core.Common.ClassConsts.Action;

namespace Domic.Core.Persistence.Interceptors;

public class EfOutBoxPublishEventInterceptor<TIdentity> : SaveChangesInterceptor
{
    private readonly ISerializer              _serializer;
    private readonly IConfiguration           _configuration;
    private readonly IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    public EfOutBoxPublishEventInterceptor(IGlobalUniqueIdGenerator globalUniqueIdGenerator, ISerializer serializer, 
        IConfiguration configuration
    )
    {
        _serializer              = serializer;
        _configuration           = configuration;
        _globalUniqueIdGenerator = globalUniqueIdGenerator;
    }
    
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            var outBoxEvents = new List<Event>();

            var entries = eventData.Context.ChangeTracker.Entries<Entity<TIdentity>>();

            foreach (var entry in entries)
            {
                var domainEntity = entry.Entity;
                var domainEvents = domainEntity.GetEvents;

                outBoxEvents.AddRange(
                    domainEvents.Select(@event => new Event {
                        Id      = _globalUniqueIdGenerator.GetRandom(6),
                        Type    = @event.GetType().Name,
                        //Target Service => ( WebAPI ) project => ( Configs ) folder => Service.json
                        Service = _configuration.GetValue<string>("NameOfService"),
                        Table   = $"{domainEntity.GetType().Name}Table",
                        Payload = _serializer.Serialize( @event ),
                        User    = domainEntity.UpdatedBy?.ToString() ?? domainEntity.CreatedBy.ToString(),
                        Action  = entry.State switch {
                            EntityState.Added    => Action.Create ,
                            EntityState.Modified => Action.Update ,
                            EntityState.Deleted  => Action.Delete ,
                            _ => "Unknown"
                        },
                        CreatedAt_EnglishDate = domainEntity.CreatedAt.EnglishDate.Value,
                        CreatedAt_PersianDate = domainEntity.CreatedAt.PersianDate
                    })
                );
            }
            
            eventData.Context.Set<Event>().AddRange(outBoxEvents);
        }
        
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = new CancellationToken()
    )
    {
        if (eventData.Context is not null && !cancellationToken.IsCancellationRequested)
        {
            var outBoxEvents = new List<Event>();

            var entries = eventData.Context.ChangeTracker.Entries<Entity<TIdentity>>();

            foreach (var entry in entries)
            {
                var domainEntity = entry.Entity;
                var domainEvents = domainEntity.GetEvents;

                outBoxEvents.AddRange(
                    domainEvents.Select(@event => new Event {
                        Id      = _globalUniqueIdGenerator.GetRandom(6),
                        Type    = @event.GetType().Name,
                        //Target Service => ( WebAPI ) project => ( Configs ) folder => Service.json
                        Service = _configuration.GetValue<string>("NameOfService"),
                        Table   = $"{domainEntity.GetType().Name}Table",
                        Payload = _serializer.Serialize( @event ),
                        User    = domainEntity.UpdatedBy?.ToString() ?? domainEntity.CreatedBy.ToString(),
                        Action  = entry.State switch {
                            EntityState.Added    => Action.Create ,
                            EntityState.Modified => Action.Update ,
                            EntityState.Deleted  => Action.Delete ,
                            _ => "Unknown"
                        },
                        CreatedAt_EnglishDate = domainEntity.CreatedAt.EnglishDate.Value,
                        CreatedAt_PersianDate = domainEntity.CreatedAt.PersianDate
                    })
                );
            }
            
            eventData.Context.Set<Event>().AddRange(outBoxEvents);
        }
        
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}