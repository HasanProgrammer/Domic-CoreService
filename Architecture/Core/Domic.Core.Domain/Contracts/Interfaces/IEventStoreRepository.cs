using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Entities;

namespace Domic.Core.Domain.Contracts.Interfaces;

public interface IEventStoreRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public List<Event> FindAll() => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<List<Event>> FindAllAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Change(Event @event) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task ChangeAsync(Event @event, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="events"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Append(IEnumerable<IDomainEvent> events) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="events"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AppendAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <typeparam name="TAggregateIdentity"></typeparam>
    /// <returns></returns>
    public IEnumerable<IDomainEvent> Load<TAggregateIdentity>(TAggregateIdentity identity);

    /// <summary>
    ///
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TAggregateIdentity"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<IDomainEvent>> LoadAsync<TAggregateIdentity>(TAggregateIdentity identity,
        CancellationToken cancellationToken
    );
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TAggregateIdentity"></typeparam>
    /// <returns></returns>
    public TEntity AssembleEntity<TEntity, TAggregateIdentity>(TAggregateIdentity identity)
        where TEntity : Entity<TAggregateIdentity>, new();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TAggregateIdentity"></typeparam>
    /// <returns></returns>
    public Task<TEntity> AssembleEntityAsync<TEntity, TAggregateIdentity>(TAggregateIdentity identity,
        CancellationToken cancellationToken
    ) where TEntity : Entity<TAggregateIdentity>, new();
}