using Domic.Core.Common.ClassExtensions;
using Domic.Core.Domain.Constants;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.WebAPI.Exceptions;
using Grpc.Core;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Extensions;

public static class ServerCallContextExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <exception cref="PresentationException"></exception>
    public static void CheckLicense(this ServerCallContext context, IConfiguration configuration)
    {
        var headers = context.GetHttpContext().Request.Headers;
        
        if(!headers.Licence().Equals( configuration.GetValue<string>("SecretKey") )) 
            throw new PresentationException("شما مجوز لازم برای دسترسی به این منبع را دارا نمی باشید !");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="externalMessageBroker"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    public static void CentralRequestLogger(this ServerCallContext context, IExternalMessageBroker externalMessageBroker,
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IDateTime dateTime, string serviceName, object payload
    )
    {
        var httpContext = context.GetHttpContext();
            
        var nowDateTime        = DateTime.Now;
        var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
        var systemRequest = new SystemRequest {
            Id        = globalUniqueIdGenerator.GetRandom(6)    ,
            IpClient  = httpContext.GetClientIP()               ,
            Service   = serviceName                             ,
            Action    = context.Method                          ,
            Header    = httpContext.Request.Headers.Serialize() ,
            Payload   = payload.Serialize()                     ,
            CreatedAt_EnglishDate = nowDateTime                 ,
            CreatedAt_PersianDate = nowPersianDateTime
        };
            
        var dto = new MessageBrokerDto<SystemRequest> {
            Message      = systemRequest,
            ExchangeType = Exchange.Direct,
            Exchange     = Broker.Request_Exchange,
            Route        = Broker.StateTracker_Request_Route
        };
            
        externalMessageBroker.Publish<SystemRequest>(dto);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="externalMessageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this ServerCallContext context, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IExternalMessageBroker externalMessageBroker, IDateTime dateTime, 
        string serviceName, object payload, CancellationToken cancellationToken
    )
    {
        try
        {
            var httpContext = context.GetHttpContext();
                
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = globalUniqueIdGenerator.GetRandom(6)    ,
                IpClient  = httpContext.GetClientIP()               ,
                Service   = serviceName                             ,
                Action    = context.Method                          ,
                Header    = httpContext.Request.Headers.Serialize() ,
                Payload   = payload.Serialize()                     ,
                CreatedAt_EnglishDate = nowDateTime                 ,
                CreatedAt_PersianDate = nowPersianDateTime
            };
            
            var dto = new MessageBrokerDto<SystemRequest> {
                Message      = systemRequest,
                ExchangeType = Exchange.Direct,
                Exchange     = Broker.Request_Exchange,
                Route        = Broker.StateTracker_Request_Route
            };
            
            await Task.Run(() => externalMessageBroker.Publish<SystemRequest>(dto), cancellationToken);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, serviceName, 
                context.Method
            );
            
            e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, externalMessageBroker, dateTime, serviceName, 
                context.Method
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="streamBroker"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    public static void CentralRequestLoggerAsStream(this ServerCallContext context, IExternalEventStreamBroker streamBroker,
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IDateTime dateTime, string serviceName, object payload
    )
    {
        var httpContext = context.GetHttpContext();
            
        var nowDateTime        = DateTime.Now;
        var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
        var systemRequest = new SystemRequest {
            Id        = globalUniqueIdGenerator.GetRandom(6)    ,
            IpClient  = httpContext.GetClientIP()               ,
            Service   = serviceName                             ,
            Action    = context.Method                          ,
            Header    = httpContext.Request.Headers.Serialize() ,
            Payload   = payload.Serialize()                     ,
            CreatedAt_EnglishDate = nowDateTime                 ,
            CreatedAt_PersianDate = nowPersianDateTime
        };
            
        streamBroker.Publish<SystemRequest>("StateTracker", systemRequest);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="externalEventStreamBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task CentralRequestLoggerAsStreamAsync(this ServerCallContext context, 
        IHostEnvironment hostEnvironment, IGlobalUniqueIdGenerator globalUniqueIdGenerator, 
        IExternalEventStreamBroker externalEventStreamBroker, IDateTime dateTime, string serviceName, object payload, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            var httpContext = context.GetHttpContext();
                
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = globalUniqueIdGenerator.GetRandom(6)    ,
                IpClient  = httpContext.GetClientIP()               ,
                Service   = serviceName                             ,
                Action    = context.Method                          ,
                Header    = httpContext.Request.Headers.Serialize() ,
                Payload   = payload.Serialize()                     ,
                CreatedAt_EnglishDate = nowDateTime                 ,
                CreatedAt_PersianDate = nowPersianDateTime
            };
            
            await Task.Run(() => externalEventStreamBroker.Publish<SystemRequest>("StateTracker", systemRequest), cancellationToken);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, serviceName, 
                context.Method
            );
            
            e.CentralExceptionLoggerAsStream(hostEnvironment, globalUniqueIdGenerator, externalEventStreamBroker, dateTime,
                serviceName, context.Method
            );
        }
    }
}