using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class EntityQuery<TIdentity> : BaseEntityQuery<TIdentity>
{
    public IsActive IsActive              { get; set; } = IsActive.Active;
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate   { get; set; }
    public DateTime UpdatedAt_EnglishDate { get; set; }
    public string UpdatedAt_PersianDate   { get; set; }
}