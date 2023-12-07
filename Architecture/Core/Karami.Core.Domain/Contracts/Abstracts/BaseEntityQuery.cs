using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class BaseEntityQuery<TIdentity>
{
    public TIdentity Id        { get; set; }
    public IsDeleted IsDeleted { get; set; } = IsDeleted.UnDelete;
}