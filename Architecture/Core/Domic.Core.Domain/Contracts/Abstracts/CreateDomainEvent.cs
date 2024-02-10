using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.Domain.Contracts.Abstracts;

public abstract class CreateDomainEvent<TDomainIdentity> : IDomainEvent
{
    public required TDomainIdentity Id             { get; init; }
    public required TDomainIdentity CreatedBy      { get; init; }
    public required string CreatedRole             { get; init; }
    public required DateTime CreatedAt_EnglishDate { get; init; }
    public required string CreatedAt_PersianDate   { get; init; }
}