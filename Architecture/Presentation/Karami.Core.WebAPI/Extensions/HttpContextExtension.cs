﻿using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Karami.Core.UseCase.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.WebAPI.Extensions;

public static class HttpContextExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="Configuration"></param>
    /// <returns></returns>
    public static HttpContext AddCorsHeaders(this HttpContext Context, IConfiguration Configuration)
    {
        Context.Response.Headers.Add("Access-Control-Allow-Origin", Configuration.GetValue<string>("AdminWebUrl"));

        return Context;
    }

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
    /// <returns></returns>
    public static HttpContext ClearHeaders(this HttpContext Context)
    {
        Context.Response.Headers.Clear();

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
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetTokenFromQueryString(this HttpContext Context) 
        => $"Bearer { Context.Request.Query["access_token"] }";
   
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetRowTokenFromQueryString(this HttpContext Context) => Context.Request.Query["access_token"];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static void CheckLicense(this HttpContext context, IConfiguration configuration)
    {
        var headers = context.Request.Headers;
        
        if(!headers.Licence().Equals( configuration.GetValue<string>("SecretKey") )) 
            throw new InvalidOperationException("شما مجوز لازم برای دسترسی به این منبع را دارا نمی باشید !");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="serviceName"></param>
    public static void CentralRequestLogger(this HttpContext context, IHostEnvironment hostEnvironment, 
        IMessageBroker messageBroker, IDotrisDateTime dotrisDateTime, string serviceName
    )
    {
        try
        {
            var httpRequest = context.Request;
            
            if(!httpRequest.Body.CanSeek)
                httpRequest.EnableBuffering();

            httpRequest.Body.Position = 0;

            StreamReader streamReader = new(httpRequest.Body);

            var payload = streamReader.ReadToEndAsync().Result;
            
            httpRequest.Body.Position = 0;

            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = Guid.NewGuid().ToString()           ,
                IpClient  = context.GetClientIP()               ,
                Service   = serviceName                         ,
                Action    = context.Request.Path                ,
                Header    = context.Request.Headers.Serialize() ,
                Payload   = payload                             ,
                CreatedAt_EnglishDate = nowDateTime             ,
                CreatedAt_PersianDate = nowPersianDateTime      ,
                UpdatedAt_EnglishDate = nowDateTime             ,
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
            e.FileLogger(hostEnvironment, dotrisDateTime);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralRequestLoggerAsync(this HttpContext context, IHostEnvironment hostEnvironment, 
        IMessageBroker messageBroker, IDotrisDateTime dotrisDateTime, string serviceName, 
        CancellationToken cancellationToken
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
            var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
            
            var systemRequest = new SystemRequest {
                Id        = Guid.NewGuid().ToString()           ,
                IpClient  = context.GetClientIP()               ,
                Service   = serviceName                         ,
                Action    = context.Request.Path                ,
                Header    = context.Request.Headers.Serialize() ,
                Payload   = payload                             ,
                CreatedAt_EnglishDate = nowDateTime             ,
                CreatedAt_PersianDate = nowPersianDateTime      ,
                UpdatedAt_EnglishDate = nowDateTime             ,
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
            e.FileLogger(hostEnvironment, dotrisDateTime);
        }
    }
}