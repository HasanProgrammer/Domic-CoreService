using Karami.Core.Domain.Contracts.Abstracts;

namespace Karami.Core.Domain.Contracts.Interfaces;

//Write side operations

public interface ICommandRepository<TEntity, TIdentity> : IRepository<TEntity> where TEntity : BaseEntity<TIdentity>
{
    
}