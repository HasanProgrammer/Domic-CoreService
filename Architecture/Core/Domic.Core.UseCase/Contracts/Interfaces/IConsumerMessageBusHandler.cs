﻿namespace Domic.Core.UseCase.Contracts.Interfaces;

/// <summary>
/// This contract just used in ( StateTrackerService )
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IConsumerMessageBusHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Handle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task HandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandle(TMessage message) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void AfterMaxRetryHandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}