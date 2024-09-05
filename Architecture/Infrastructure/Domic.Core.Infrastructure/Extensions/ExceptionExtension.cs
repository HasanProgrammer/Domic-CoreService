#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Domic.Core.Domain.Constants;
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
    private static SemaphoreSlim _asyncLock = new(1, 1);
    
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
    /// <param name="exception"></param>
    /// <param name="environment"></param>
    /// <param name="dateTime"></param>
    /// <param name="cancellationToken"></param>
    public static async Task FileLoggerAsync(this Exception exception, IHostEnvironment environment, IDateTime dateTime, 
        CancellationToken cancellationToken
    )
    {
        await _asyncLock.WaitAsync(cancellationToken);
        
        try
        {
            string logsPath = Path.Combine(environment.ContentRootPath, "CoreLogs", "Logs.txt");

            if (!File.Exists(logsPath))
                File.Create(logsPath);
        
            await using StreamWriter streamWriter = new(logsPath, append: true);

            await streamWriter.WriteLineAsync(
                $"\n Date: {dateTime.ToPersianShortDate(DateTime.Now)} | Message: {exception.Message} | Source: {exception.ToString()} \n"
            );
        }
        catch (Exception e) {}
        finally
        {
            _asyncLock.Release();
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
            //fire&forget
            exception.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="globalUniqueIdGenerator"></param>
    /// <param name="eventStreamBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void CentralExceptionLoggerAsStream(this Exception e, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IEventStreamBroker eventStreamBroker, IDateTime dateTime, 
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
            
            eventStreamBroker.Publish<SystemException>("StateTracker", systemException);
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
    /// <param name="eventStreamBroker"></param>
    /// <param name="dateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralExceptionLoggerAsStreamAsync(this Exception e, IHostEnvironment hostEnvironment, 
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, IEventStreamBroker eventStreamBroker, IDateTime dateTime, 
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
            
            await eventStreamBroker.PublishAsync<SystemException>("StateTracker", systemException, 
                cancellationToken: cancellationToken
            );
        }
        catch (Exception exception)
        {
            //fire&forget
            exception.FileLoggerAsync(hostEnvironment, dateTime, cancellationToken);
        }
    }
}