#pragma warning disable CS0649
#pragma warning disable CS8632
#pragma warning disable CS0659

using System.Collections.ObjectModel;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.ValueObjects;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract partial class Entity<TIdentity> : BaseEntity<TIdentity>
{
    public IsActive  IsActive   { get; protected set; } = IsActive.Active;
    public CreatedAt CreatedAt  { get; protected set; }
    public UpdatedAt UpdatedAt  { get; protected set; }
}

public abstract partial class Entity<TIdentity>
{
    private readonly List<IDomainEvent> _Events;

    /// <summary>
    /// 
    /// </summary>
    protected Entity() => _Events = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    protected void AddEvent(IDomainEvent @event) => _Events.Add(@event);

    /// <summary>
    /// 
    /// </summary>
    public ReadOnlyCollection<IDomainEvent> GetEvents => _Events.AsReadOnly();

    /// <summary>
    /// 
    /// </summary>
    public void ClearEvents() => _Events.Clear();
}