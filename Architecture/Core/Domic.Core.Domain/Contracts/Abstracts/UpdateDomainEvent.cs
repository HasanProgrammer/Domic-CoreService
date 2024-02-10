using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.Domain.Contracts.Abstracts;

public abstract class UpdateDomainEvent<TDomainIdentity> : IDomainEvent
{
    public required TDomainIdentity Id             { get; init; }
    public required TDomainIdentity UpdatedBy      { get; init; }
    public required string UpdatedRole             { get; init; }
    public required DateTime UpdatedAt_EnglishDate { get; init; }
    public required string UpdatedAt_PersianDate   { get; init; }
}