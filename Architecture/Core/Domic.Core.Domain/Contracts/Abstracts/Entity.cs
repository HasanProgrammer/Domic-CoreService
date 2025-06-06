#pragma warning disable CS0649
#pragma warning disable CS8632
#pragma warning disable CS0659

using System.Collections.ObjectModel;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Domain.ValueObjects;

namespace Domic.Core.Domain.Contracts.Abstracts;

public abstract partial class Entity<TIdentity> : BaseEntity<TIdentity>
{
    public IsActive IsActive    { get; protected set; } = IsActive.Active;
    public CreatedAt CreatedAt  { get; protected set; }
    public UpdatedAt? UpdatedAt { get; protected set; }
}

public abstract partial class Entity<TIdentity>
{
    private readonly List<IDomainEvent> _events;

    /// <summary>
    /// 
    /// </summary>
    protected Entity() => _events = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    protected void AddEvent(IDomainEvent @event) => _events.Add(@event);

    /// <summary>
    /// 
    /// </summary>
    public ReadOnlyCollection<IDomainEvent> GetEvents => _events.AsReadOnly();

    /// <summary>
    /// 
    /// </summary>
    public void ClearEvents() => _events.Clear();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    public void Apply(IDomainEvent @event){}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="events"></param>
    public void ApplyAll(IEnumerable<IDomainEvent> events){}
}