using System.Linq.Expressions;
using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Contracts.Interfaces;

//Transaction
public partial interface IRepository<TEntity>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Add(TEntity entity) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AddAsync(TEntity entity, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /*-----------------------------------------------------------*/

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Change(TEntity entity) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task ChangeAsync(TEntity entity, CancellationToken cancellationToken) => throw new NotImplementedException();

    /*-----------------------------------------------------------*/
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Remove(object id) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task RemoveAsync(object id, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Remove(TEntity entity) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task RemoveAsync(TEntity entity, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveRange(IEnumerable<object> ids) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task RemoveRangeAsync(IEnumerable<object> ids, CancellationToken cancellationToken)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveRange(IEnumerable<TEntity> entities) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}

//Query
public partial interface IRepository<TEntity>
{
    #region Single

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindById(object id) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdAsync(object id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdByProjection<TViewModel>(Expression<Func<TEntity, TViewModel>> projection, object id) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdByProjectionAsync<TViewModel>(Expression<Func<TEntity, TViewModel>> projection,
        object id, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdConditionally(object id, Expression<Func<TEntity, bool>> condition) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdConditionally(object id, params Expression<Func<TEntity, bool>>[] conditions) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="condition"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdConditionallyAsync(object id, Expression<Func<TEntity, bool>> condition,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdConditionallyAsync(object id, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdByProjectionConditionally<TViewModel>(object id, 
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdByProjectionConditionally<TViewModel>(object id, 
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdByProjectionConditionallyAsync<TViewModel>(object id, 
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdByProjectionConditionallyAsync<TViewModel>(object id, 
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-EagerLoading

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdEagerLoading(object id) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdEagerLoadingAsync(object id, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-EagerLoading-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdEagerLoadingByProjection<TViewModel>(Expression<Func<TEntity, TViewModel>> projection,
        object id
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, object id, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-EagerLoading-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdEagerLoadingConditionally(object id, Expression<Func<TEntity, bool>> condition)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdEagerLoadingConditionally(object id, params Expression<Func<TEntity, bool>>[] conditions)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdEagerLoadingConditionallyAsync(object id, CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdEagerLoadingConditionallyAsync(object id, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-EagerLoading-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdEagerLoadingByProjectionConditionally<TViewModel>(object id,
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdEagerLoadingByProjectionConditionally<TViewModel>(object id,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdEagerLoadingByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdEagerLoadingByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActive(object id) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveAsync(object id, CancellationToken cancellationToken) 
        => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveByProjection<TViewModel>(Expression<Func<TEntity, TViewModel>> projection,
        object id
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, object id, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActiveConditionally(object id, Expression<Func<TEntity, bool>> condition)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActiveConditionally(object id, params Expression<Func<TEntity, bool>>[] conditions)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveConditionallyAsync(object id, CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveConditionallyAsync(object id, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveByProjectionConditionally<TViewModel>(object id, 
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveByProjectionConditionally<TViewModel>(object id,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-EagerLoading

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActiveEagerLoading(object id) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveEagerLoadingAsync(object id, CancellationToken cancellationToken) 
        => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-EagerLoading-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveEagerLoadingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, object id
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, object id, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-EagerLoading-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActiveEagerLoadingConditionally(object id, Expression<Func<TEntity, bool>> condition) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TEntity FindByIdActiveEagerLoadingConditionally(object id, params Expression<Func<TEntity, bool>>[] conditions) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveEagerLoadingConditionallyAsync(object id, CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TEntity> FindByIdActiveEagerLoadingConditionallyAsync(object id, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Single-Active-EagerLoading-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveEagerLoadingByProjectionConditionally<TViewModel>(object id, 
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TViewModel FindByIdActiveEagerLoadingByProjectionConditionally<TViewModel>(object id,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveEagerLoadingByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TViewModel> FindByIdActiveEagerLoadingByProjectionConditionallyAsync<TViewModel>(object id,
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAll() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllAsync(CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithOrdering(Order order, bool accending = true) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithOrderingAsync(Order order, bool accending, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllByProjection<TViewModel>(Expression<Func<TEntity, TViewModel>> projection)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending = true
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllConditionally(Expression<Func<TEntity, bool>> condition)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllConditionally(params Expression<Func<TEntity, bool>>[] conditions)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllConditionallyAsync(CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllConditionallyAsync(CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="condition"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithOrderingConditionally(Order order, 
        Expression<Func<TEntity, bool>> condition, bool accending = true
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithOrderingConditionally(Order order, bool accending = true,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithOrderingConditionallyAsync(Order order, bool accending, 
        CancellationToken cancellationToken, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithOrderingConditionallyAsync(Order order, bool accending, 
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection, 
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection, 
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithOrderingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition,
        Order order, bool accending = true
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithOrderingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection,
        Order order, bool accending = true, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithOrderingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Order order, bool accending,
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithOrderingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Order order, bool accending,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-EagerLoading-Ordering

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoading() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingAsync(CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoadingWithOrdering(Order order, bool accending) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingWithOrderingAsync(Order order, bool accending, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/

    #region All-EagerLoading-Ordering-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingWithOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingWithOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/
    
    #region All-EagerLoading-Ordering-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoadingConditionally(Expression<Func<TEntity, bool>> condition) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoadingConditionally(params Expression<Func<TEntity, bool>>[] conditions) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingConditionallyAsync(CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingConditionallyAsync(CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoadingWithOrderingConditionally(Order order, bool accending, 
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllEagerLoadingWithOrderingConditionally(Order order, bool accending, 
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllEagerLoadingWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-EagerLoading-Ordering-Condition-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingWithOrderingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition,
        Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllEagerLoadingWithOrderingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllEagerLoadingWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActive() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveAsync(CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithOrdering(Order order, bool accending = true) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithOrderingAsync(Order order, bool accending, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending = true
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/
    
    #region All-Active-Ordering-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveConditionally(Expression<Func<TEntity, bool>> condition)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveConditionally(params Expression<Func<TEntity, bool>>[] conditions)
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveConditionallyAsync(CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithOrderingConditionally(Order order, bool accending,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithOrderingConditionally(Order order, bool accending,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithOrderingByProjectionConditionally<TViewModel>(
        Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithOrderingByProjectionConditionally<TViewModel>(
        Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-EagerLoading

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoading() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingAsync(CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoadingWithOrdering(Order order, bool accending = true) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingWithOrderingAsync(Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-EagerLoading-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingWithOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending = true
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingWithOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/
    
    #region All-Active-Ordering-EagerLoading-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoadingConditionally(Expression<Func<TEntity, bool>> condition) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoadingConditionally(params Expression<Func<TEntity, bool>>[] condition) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingConditionallyAsync(CancellationToken cancellationToken,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingConditionallyAsync(CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoadingWithOrderingConditionally(Order order, bool accending,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveEagerLoadingWithOrderingConditionally(Order order, bool accending,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveEagerLoadingWithOrderingConditionallyAsync(Order order, bool accending,
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/
    
    #region All-Active-Ordering-EagerLoading-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingByProjectionConditionally<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingByProjectionConditionallyAsync<TViewModel>(
        CancellationToken cancellationToken, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingWithOrderingByProjectionConditionally<TViewModel>(
        Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveEagerLoadingWithOrderingByProjectionConditionally<TViewModel>(
        Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="condition"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, Expression<Func<TEntity, bool>> condition
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveEagerLoadingWithOrderingByProjectionConditionallyAsync<TViewModel>(
        Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Pagination

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginate(int countPerPage, int pageNumber) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateAndOrdering(int countPerPage, int pageNumber, Order order, 
        bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateAndOrderingAsync(int countPerPage, int pageNumber, Order order,
        bool accending, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/

    #region All-Ordering-Pagination-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateAndOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateAndOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion

    /*-----------------------------------------------------------*/

    #region All-Ordering-Pagination-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateConditionally(int countPerPage, int pageNumber,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateConditionallyAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateAndOrderingConditionally(int countPerPage, int pageNumber, 
        Order order, bool accending, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateAndOrderingConditionallyAsync(int countPerPage, int pageNumber,
        Order order, bool accending, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/
    
    #region All-Ordering-Pagination-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateAndOrderingByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateAndOrderingByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Pagination-EagerLoading

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateEagerLoading(int countPerPage, int pageNumber) 
        => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateEagerLoadingAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateEagerLoadingAndOrdering(int countPerPage, int pageNumber, Order order,
        bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateEagerLoadingAndOrderingAsync(int countPerPage, int pageNumber, 
        Order order, bool accending, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Ordering-Pagination-EagerLoading-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateEagerLoadingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber
    ) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllWithPaginateEagerLoadingAndOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllWithPaginateEagerLoadingAndOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/
    
    #region All-Ordering-Pagination-EagerLoading-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateEagerLoadingConditionally(int countPerPage, int pageNumber,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateEagerLoadingConditionallyAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllWithPaginateEagerLoadingAndOrderingConditionally(int countPerPage, 
        int pageNumber, Order order, bool accending, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllWithPaginateEagerLoadingAndOrderingConditionallyAsync(int countPerPage, 
        int pageNumber, Order order, bool accending, CancellationToken cancellationToken, 
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginate(int countPerPage, int pageNumber) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateAndOrdering(int countPerPage, int pageNumber, Order order,
        bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateAndOrderingAsync(int countPerPage, int pageNumber,
        Order order, bool accending, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateAndOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateAndOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateConditionally(int countPerPage, int pageNumber,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateConditionallyAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateAndOrderingConditionally(int countPerPage, int pageNumber,
        Order order, bool accending, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateAndOrderingConditionallyAsync(int countPerPage,
        int pageNumber, Order order, bool accending, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateAndOrderingByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateAndOrderingByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-EagerLoading

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateEagerLoading(int countPerPage, int pageNumber) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateEagerLoadingAsync(int countPerPage, int pageNumber, 
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateEagerLoadingAndOrdering(int countPerPage, int pageNumber, 
        Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateEagerLoadingAndOrderingAsync(int countPerPage, 
        int pageNumber, Order order, bool accending, CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-EagerLoading-Projection

    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateEagerLoadingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateEagerLoadingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateEagerLoadingAndOrderingByProjection<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projection"></param>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateEagerLoadingAndOrderingByProjectionAsync<TViewModel>(
        Expression<Func<TEntity, TViewModel>> projection, int countPerPage, int pageNumber, Order order, bool accending,
        CancellationToken cancellationToken
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-EagerLoading-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateEagerLoadingConditionally(int countPerPage, int pageNumber,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateEagerLoadingConditionallyAsync(int countPerPage,
        int pageNumber, CancellationToken cancellationToken, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TEntity> FindAllActiveWithPaginateEagerLoadingAndOrderingConditionally(int countPerPage, 
        int pageNumber, Order order, bool accending, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="conditions"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TEntity>> FindAllActiveWithPaginateEagerLoadingAndOrderingConditionallyAsync(
        int countPerPage, int pageNumber, Order order, bool accending, CancellationToken cancellationToken,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region All-Active-Ordering-Pagination-EagerLoading-Projection-Condition

    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateEagerLoadingByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>> FindAllActiveWithPaginateEagerLoadingByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IEnumerable<TViewModel> FindAllActiveWithPaginateEagerLoadingAndOrderingByProjectionConditionally<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending, Expression<Func<TEntity, TViewModel>> projection,
        params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="countPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="order"></param>
    /// <param name="accending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="projection"></param>
    /// <param name="conditions"></param>
    /// <typeparam name="TViewModel"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<IEnumerable<TViewModel>>
    FindAllActiveWithPaginateEagerLoadingAndOrderingByProjectionConditionallyAsync<TViewModel>(
        int countPerPage, int pageNumber, Order order, bool accending, CancellationToken cancellationToken,
        Expression<Func<TEntity, TViewModel>> projection, params Expression<Func<TEntity, bool>>[] conditions
    ) => throw new NotImplementedException();

    #endregion
    
    /*-----------------------------------------------------------*/

    #region Count

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public long CountRows() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public ValueTask<long> CountRowsAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

    #endregion
}