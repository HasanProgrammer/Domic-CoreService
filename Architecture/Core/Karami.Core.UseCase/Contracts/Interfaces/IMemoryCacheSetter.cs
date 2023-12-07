﻿namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IMemoryCacheSetter<TResult>
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public TResult Set() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<TResult> SetAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}