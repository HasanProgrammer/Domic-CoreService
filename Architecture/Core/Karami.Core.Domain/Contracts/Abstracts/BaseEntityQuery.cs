﻿#pragma warning disable CS8632

using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class BaseEntityQuery<TIdentity>
{
    public TIdentity Id         { get; set; }
    public TIdentity CreatedBy  { get; set; }
    public TIdentity? UpdatedBy { get; set; }
    public string CreatedRole   { get; set; }
    public string UpdatedRole   { get; set; }
    public IsDeleted IsDeleted  { get; set; } = IsDeleted.UnDelete;
    public string Version       { get; } = Guid.NewGuid().ToString();
}