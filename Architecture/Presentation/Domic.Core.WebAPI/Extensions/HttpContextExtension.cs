#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Domic.Core.Domain.Constants;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Extensions;

public static class HttpContextExtension
{
    private const string StateTrackerTopic = "StateTracker";
    
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
    /// <param name="externalMessageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    public static void CentralRequestLogger(this HttpContext context, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IExternalMessageBroker externalMessageBroker, IDateTime dateTime, 
        string serviceName
    )
    {
        var httpRequest = context.Request;
            
        httpRequest.EnableBuffering(bufferLimit: int.MaxValue);

        httpRequest.Body.Position = 0;

        StreamReader streamReader = new(httpRequest.Body, leaveOpen: true);

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
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this HttpContext context, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IExternalMessageBroker externalMessageBroker, IDateTime dateTime, 
        string serviceName, CancellationToken cancellationToken
    )
    {
        try
        {
            var httpRequest = context.Request;
            
            httpRequest.EnableBuffering(bufferLimit: int.MaxValue);

            httpRequest.Body.Position = 0;

            StreamReader streamReader = new(httpRequest.Body, leaveOpen: true);

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

            await externalMessageBroker.PublishAsync<SystemRequest>(dto, cancellationToken);
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, serviceName, 
                context.Request.Path
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="externalEventStreamBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="serviceName"></param>
    public static void CentralRequestLoggerAsStream(this HttpContext context, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IExternalEventStreamBroker externalEventStreamBroker, 
        IDateTime dateTime, string serviceName
    )
    {
        var httpRequest = context.Request;
            
        httpRequest.EnableBuffering(bufferLimit: int.MaxValue);

        httpRequest.Body.Position = 0;

        StreamReader streamReader = new(httpRequest.Body, leaveOpen: true);

        var payload = streamReader.ReadToEndAsync().GetAwaiter().GetResult();
            
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
                
        externalEventStreamBroker.Publish<SystemRequest>(StateTrackerTopic, systemRequest);
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
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsStreamAsync(this HttpContext context, 
        IHostEnvironment hostEnvironment, IGlobalUniqueIdGenerator globalUniqueIdGenerator, 
        IExternalEventStreamBroker externalEventStreamBroker, IDateTime dateTime, string serviceName, 
        CancellationToken cancellationToken
    )
    {
        try
        {
            var httpRequest = context.Request;
            
            httpRequest.EnableBuffering(bufferLimit: int.MaxValue);

            httpRequest.Body.Position = 0;

            StreamReader streamReader = new(httpRequest.Body, leaveOpen: true);

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

            await externalEventStreamBroker.PublishAsync<SystemRequest>(StateTrackerTopic, systemRequest, 
                cancellationToken: cancellationToken
            );
        }
        catch (Exception e)
        {
            //fire&forget
            e.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
            
            e.ElasticStackExceptionLogger(hostEnvironment, globalUniqueIdGenerator, dateTime, serviceName, 
                context.Request.Path
            );
        }
    }
}