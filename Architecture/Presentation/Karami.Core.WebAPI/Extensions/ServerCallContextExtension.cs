using Grpc.Core;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Karami.Core.WebAPI.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using ILogger = Serilog.ILogger;

namespace Karami.Core.WebAPI.Extensions;

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
    /// <param name="messageBroker"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    public static void CentralRequestLogger(this ServerCallContext context, IMessageBroker messageBroker,
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
            
        messageBroker.Publish<SystemRequest>(dto);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="logger"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this ServerCallContext context, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IMessageBroker messageBroker, IDateTime dateTime, 
        ILogger logger, string serviceName, object payload, CancellationToken cancellationToken
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
            
            await Task.Run(() => messageBroker.Publish<SystemRequest>(dto), cancellationToken);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, logger, serviceName, 
                context.Method
            );
            
            e.CentralExceptionLogger(hostEnvironment, globalUniqueIdGenerator, messageBroker, dateTime, serviceName, 
                context.Method
            );
        }
    }
}