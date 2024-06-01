using Domic.Core.Domain.Contracts.Abstracts;

namespace Domic.Core.Domain.Contracts.Interfaces;

//Read side operations

public interface IQueryRepository<TEntity, TIdentity> : IRepository<TEntity> where TEntity : BaseEntityQuery<TIdentity>;