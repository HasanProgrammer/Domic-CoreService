﻿namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IConsumerMessageStreamHandler<in TMessage> where TMessage : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public void Handle(TMessage message);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task HandleAsync(TMessage @event, CancellationToken cancellationToken) => throw new NotImplementedException();
    
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
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task AfterMaxRetryHandleAsync(TMessage message, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}