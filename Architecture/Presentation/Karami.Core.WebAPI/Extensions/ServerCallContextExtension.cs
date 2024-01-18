using Grpc.Core;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Karami.Core.UseCase.Extensions;
using Karami.Core.WebAPI.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.WebAPI.Extensions;

public static class ServerCallContextExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
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
    /// <param name="dateTime"></param>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    public static void CentralRequestLogger(this ServerCallContext context, IMessageBroker messageBroker,
        IDateTime dateTime, IHostEnvironment hostEnvironment,
        string serviceName, object payload
    )
    {
        try
        {
            var httpContext = context.GetHttpContext();
            
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = Guid.NewGuid().ToString()               ,
                IpClient  = httpContext.GetClientIP()               ,
                Service   = serviceName                             ,
                Action    = context.Method                          ,
                Header    = httpContext.Request.Headers.Serialize() ,
                Payload   = payload.Serialize()                     ,
                CreatedAt_EnglishDate = nowDateTime                 ,
                CreatedAt_PersianDate = nowPersianDateTime          ,
                UpdatedAt_EnglishDate = nowDateTime                 ,
                UpdatedAt_PersianDate = nowPersianDateTime
            };
            
            var dto = new MessageBrokerDto<SystemRequest> {
                Message      = systemRequest,
                ExchangeType = Exchange.Direct,
                Exchange     = Broker.Request_Exchange,
                Route        = Broker.StateTracker_Request_Route
            };
            
            messageBroker.Publish<SystemRequest>(dto);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dateTime);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="serviceName"></param>
    /// <param name="payload"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this ServerCallContext context, 
        IMessageBroker messageBroker, IDateTime dotrisDateTime, IHostEnvironment hostEnvironment, 
        string serviceName, object payload, CancellationToken cancellationToken
    )
    {
        try
        {
            await Task.Run(() => {

                var httpContext = context.GetHttpContext();
                
                var nowDateTime        = DateTime.Now;
                var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
            
                var systemRequest = new SystemRequest {
                    Id        = Guid.NewGuid().ToString()               ,
                    IpClient  = httpContext.GetClientIP()               ,
                    Service   = serviceName                             ,
                    Action    = context.Method                          ,
                    Header    = httpContext.Request.Headers.Serialize() ,
                    Payload   = payload.Serialize()                     ,
                    CreatedAt_EnglishDate = nowDateTime                 ,
                    CreatedAt_PersianDate = nowPersianDateTime          ,
                    UpdatedAt_EnglishDate = nowDateTime                 ,
                    UpdatedAt_PersianDate = nowPersianDateTime
                };
            
                var dto = new MessageBrokerDto<SystemRequest> {
                    Message      = systemRequest,
                    ExchangeType = Exchange.Direct,
                    Exchange     = Broker.Request_Exchange,
                    Route        = Broker.StateTracker_Request_Route
                };
            
                messageBroker.Publish<SystemRequest>(dto);
                
            }, cancellationToken);
        }
        catch (Exception e)
        {
            e.FileLogger(hostEnvironment, dotrisDateTime);
        }
    }
}