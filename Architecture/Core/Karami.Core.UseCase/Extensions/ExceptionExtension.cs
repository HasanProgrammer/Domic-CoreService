using Karami.Core.Domain.Constants;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Enumerations;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.DTOs;
using Microsoft.Extensions.Hosting;

using SystemException = Karami.Core.Domain.Entities.SystemException;

namespace Karami.Core.UseCase.Extensions;

public static class ExceptionExtension
{
    private static object _lock = new();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="environment"></param>
    /// <param name="dotrisDateTime"></param>
    public static void FileLogger(this Exception exception, IHostEnvironment environment, IDotrisDateTime dotrisDateTime)
    {
        lock (_lock)
        {
            string logsPath = Path.Combine(environment.ContentRootPath, "CoreLogs", "Logs.txt");

            if (!File.Exists(logsPath))
                File.Create(logsPath);
        
            using StreamWriter streamWriter = new(logsPath, append: true);

            streamWriter.WriteLine($"\n Date: {dotrisDateTime.ToPersianShortDate(DateTime.Now)} | Message: {exception.Message} | Source: {exception.ToString()} \n");
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void CentralExceptionLogger(this Exception e, IHostEnvironment hostEnvironment, 
        IMessageBroker messageBroker, IDotrisDateTime dotrisDateTime, string service, string action
    )
    {
        try
        {
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
            
            var systemException = new SystemException {
                Id        = Guid.NewGuid().ToString()      ,
                Service   = service                        ,
                Action    = action                         ,
                Message   = e.Message                      ,
                Exception = e.StackTrace                   ,
                CreatedAt_EnglishDate = nowDateTime        ,
                CreatedAt_PersianDate = nowPersianDateTime ,
                UpdatedAt_EnglishDate = nowDateTime        ,
                UpdatedAt_PersianDate = nowPersianDateTime
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
            exception.FileLogger(hostEnvironment, dotrisDateTime);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="messageBroker"></param>
    /// <param name="dotrisDateTime"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    public static async Task CentralExceptionLoggerAsync(this Exception e, IHostEnvironment hostEnvironment,
        IMessageBroker messageBroker, IDotrisDateTime dotrisDateTime, string service, string action, 
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Task.Run(() => {
                
                var nowDateTime        = DateTime.Now;
                var nowPersianDateTime = dotrisDateTime.ToPersianShortDate(nowDateTime);
            
                var systemException = new SystemException {
                    Id        = Guid.NewGuid().ToString()      ,
                    Service   = service                        ,
                    Action    = action                         ,
                    Message   = e.Message                      ,
                    Exception = e.StackTrace                   ,
                    CreatedAt_EnglishDate = nowDateTime        ,
                    CreatedAt_PersianDate = nowPersianDateTime ,
                    UpdatedAt_EnglishDate = nowDateTime        ,
                    UpdatedAt_PersianDate = nowPersianDateTime
                };
            
                var dto = new MessageBrokerDto<SystemException> {
                    Message      = systemException,
                    ExchangeType = Exchange.Direct,
                    Exchange     = Broker.Exception_Exchange,
                    Route        = Broker.StateTracker_Exception_Route
                };

                messageBroker.Publish<SystemException>(dto);
                
            }, cancellationToken);
        }
        catch (Exception exception)
        {
            exception.FileLogger(hostEnvironment, dotrisDateTime);
        }
    }
}