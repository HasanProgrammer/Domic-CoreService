using Karami.Core.Domain.Contracts.Abstracts;

namespace Karami.Core.Domain.Contracts.Interfaces;

//Read side operations

public interface IQueryRepository<TEntity, TIdentity> : IRepository<TEntity> where TEntity : BaseEntityQuery<TIdentity>
{
    
}