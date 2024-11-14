﻿using Domic.Core.Domain.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerEventStreamHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    public void Handle(TEvent @event);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task HandleAsync(TEvent @event, CancellationToken cancellationToken) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    public void AfterTransactionHandle(TEvent @event);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task AfterTransactionHandleAsync(TEvent @event, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TEvent @event) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterMaxRetryHandleAsync(TEvent @event, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}