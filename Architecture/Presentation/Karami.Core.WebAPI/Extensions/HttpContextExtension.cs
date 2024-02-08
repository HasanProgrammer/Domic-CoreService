using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

using ILogger = Serilog.ILogger;

namespace Karami.Core.WebAPI.Extensions;

public static class HttpContextExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static HttpContext JsonContent(this HttpContext Context)
    {
        Context.Response.Headers.Add("Accept"       , "application/json");
        Context.Response.Headers.Add("Content-Type" , "application/json");

        return Context;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="StatusCode"></param>
    /// <returns></returns>
    public static HttpContext StatusCode(this HttpContext Context, int StatusCode)
    {
        Context.Response.StatusCode = StatusCode;

        return Context;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="Payload"></param>
    public static async Task SendPayloadAsync(this HttpContext Context, string Payload) 
        => await Context.Response.WriteAsync(Payload);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetClientIP(this HttpContext Context) => Context.Connection.RemoteIpAddress?.ToString();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetTokenOfGrpcHeader(this HttpContext Context) 
        => Context.Request.Headers["Token"];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    public static void CentralRequestLogger(this HttpContext context, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IMessageBroker messageBroker, IDateTime dateTime, 
        string serviceName
    )
    {
        var httpRequest = context.Request;
            
        if(!httpRequest.Body.CanSeek)
            httpRequest.EnableBuffering();

        httpRequest.Body.Position = 0;

        StreamReader streamReader = new(httpRequest.Body);

        var payload = streamReader.ReadToEndAsync().Result;
            
        httpRequest.Body.Position = 0;

        var nowDateTime        = DateTime.Now;
        var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
        var systemRequest = new SystemRequest {
            Id        = globalUniqueIdGenerator.GetRandom(6) ,
            IpClient  = context.GetClientIP()                ,
            Service   = serviceName                          ,
            Action    = context.Request.Path                 ,
            Header    = context.Request.Headers.Serialize()  ,
            Payload   = payload                              ,
            CreatedAt_EnglishDate = nowDateTime              ,
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
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this HttpContext context, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IMessageBroker messageBroker, IDateTime dateTime, 
        ILogger logger, string serviceName, CancellationToken cancellationToken
    )
    {
        try
        {
            var httpRequest = context.Request;
            
            if(!httpRequest.Body.CanSeek)
                httpRequest.EnableBuffering();

            httpRequest.Body.Position = 0;

            StreamReader streamReader = new(httpRequest.Body);

            var payload = await streamReader.ReadToEndAsync(cancellationToken);
            
            httpRequest.Body.Position = 0;
            
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = globalUniqueIdGenerator.GetRandom(6) ,
                IpClient  = context.GetClientIP()                ,
                Service   = serviceName                          ,
                Action    = context.Request.Path                 ,
                Header    = context.Request.Headers.Serialize()  ,
                Payload   = payload                              ,
                CreatedAt_EnglishDate = nowDateTime              ,
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
                context.Request.Path
            );
        }
    }
}