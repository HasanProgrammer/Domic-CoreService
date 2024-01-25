#pragma warning disable CS8632

using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class BaseEntity<TIdentity>
{
    public TIdentity Id         { get; protected set; }
    public TIdentity CreatedBy  { get; protected set; }
    public TIdentity? UpdatedBy { get; protected set; }
    public string CreatedRole   { get; protected set; }
    public string UpdatedRole   { get; protected set; }
    public IsDeleted IsDeleted  { get; protected set; } = IsDeleted.UnDelete;
    public string Version       { get; } = Guid.NewGuid().ToString();
}