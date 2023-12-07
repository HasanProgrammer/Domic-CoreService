using Karami.Core.Domain.Contracts.Interfaces;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class DeleteDomainEvent : IDomainEvent
{
    public required DateTime UpdatedAt_EnglishDate { get; init; }
    public required string UpdatedAt_PersianDate   { get; init; }
}