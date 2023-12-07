using Karami.Core.Domain.Contracts.Interfaces;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class CreateDomainEvent : IDomainEvent
{
    public required DateTime CreatedAt_EnglishDate { get; init; }
    public required string CreatedAt_PersianDate   { get; init; }
    public required DateTime UpdatedAt_EnglishDate { get; init; }
    public required string UpdatedAt_PersianDate   { get; init; }
}