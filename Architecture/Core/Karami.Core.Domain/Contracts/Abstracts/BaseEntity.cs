using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class BaseEntity<TIdentity>
{
    public TIdentity Id        { get; protected set; }
    public IsDeleted IsDeleted { get; protected set; } = IsDeleted.UnDelete;
    public string Version      { get; } = Guid.NewGuid().ToString();
}