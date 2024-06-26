﻿using Domic.Core.Domain.Constants;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Enumerations;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.Extensions.Hosting;
using Serilog;

using SystemException = Domic.Core.Domain.Entities.SystemException;

namespace Domic.Core.Infrastructure.Extensions;

public static class ExceptionExtension
{
    private static object _lock = new();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="environment"></param>
    /// <param name="dateTime"></param>
    public static void FileLogger(this Exception exception, IHostEnvironment environment, IDateTime dateTime)
    {
        lock (_lock)
        {
            string logsPath = Path.Combine(environment.ContentRootPath, "CoreLogs", "Logs.txt");

            if (!File.Exists(logsPath))
                File.Create(logsPath);
        
            using StreamWriter streamWriter = new(logsPath, append: true);

            streamWriter.WriteLine($"\n Date: {dateTime.ToPersianShortDate(DateTime.Now)} | Message: {exception.Message} | Source: {exception.ToString()} \n");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="dateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void ElasticStackExceptionLogger(this Exception e, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IDateTime dateTime, string service, string action
    )
    {
        try
        {
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemException = new SystemException {
                Id        = globalUniqueIdGenerator.GetRandom(6) ,
                Service   = service                              ,
                Action    = action                               ,
                Message   = e.Message                            ,
                Exception = e.StackTrace                         ,
                CreatedAt_EnglishDate = nowDateTime              ,
                CreatedAt_PersianDate = nowPersianDateTime
            };
            
            Log.Error($"{service}-{action}:{systemException.Serialize()}");
        }
        catch (Exception exception)
        {
            exception.FileLogger(hostEnvironment, dateTime);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void CentralExceptionLogger(this Exception e, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IMessageBroker messageBroker, IDateTime dateTime, 
        string service, string action
    )
    {
        try
        {
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemException = new SystemException {
                Id        = globalUniqueIdGenerator.GetRandom(6) ,
                Service   = service                              ,
                Action    = action                               ,
                Message   = e.Message                            ,
                Exception = e.StackTrace                         ,
                CreatedAt_EnglishDate = nowDateTime              ,
                CreatedAt_PersianDate = nowPersianDateTime
            };
            
            var dto = new MessageBrokerDto<SystemException> {
                Message      = systemException,
                ExchangeType = Exchange.Direct,
                Exchange     = Broker.Exception_Exchange,
                Route        = Broker.StateTracker_Exception_Route
            };

            //ToDo : ( Tech Debt ) -> Should be used retry pattern ( like polly )!
            messageBroker.Publish<SystemException>(dto);
        }
        catch (Exception exception)
        {
            exception.FileLogger(hostEnvironment, dateTime);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralExceptionLoggerAsync(this Exception e, IHostEnvironment hostEnvironment,
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IMessageBroker messageBroker, IDateTime dateTime, 
        string service, string action, CancellationToken cancellationToken
    )
    {
        try
        {
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
            var systemException = new SystemException {
                Id        = globalUniqueIdGenerator.GetRandom(6) ,
                Service   = service                              ,
                Action    = action                               ,
                Message   = e.Message                            ,
                Exception = e.StackTrace                         ,
                CreatedAt_EnglishDate = nowDateTime              ,
                CreatedAt_PersianDate = nowPersianDateTime
            };
            
            var dto = new MessageBrokerDto<SystemException> {
                Message      = systemException,
                ExchangeType = Exchange.Direct,
                Exchange     = Broker.Exception_Exchange,
                Route        = Broker.StateTracker_Exception_Route
            };
            
            await Task.Run(() => messageBroker.Publish<SystemException>(dto), cancellationToken);
        }
        catch (Exception exception)
        {
            exception.FileLogger(hostEnvironment, dateTime);
        }
    }
}