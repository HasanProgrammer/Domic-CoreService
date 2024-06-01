using Domic.Core.Domain.Contracts.Abstracts;

namespace Domic.Core.Domain.Contracts.Interfaces;

//Write side operations

public interface ICommandRepository<TEntity, TIdentity> : IRepository<TEntity> where TEntity : BaseEntity<TIdentity>;