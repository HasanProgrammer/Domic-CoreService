using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Extensions;
using Microsoft.Extensions.Hosting;

using ILogger         = Serilog.ILogger;
using SystemException = Karami.Core.Domain.Entities.SystemException;

namespace Karami.Core.WebAPI.Extensions;

public static class ExceptionExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="dateTime"></param>
    /// <param name="logger"></param>
    /// <param name="service"></param>
    /// <param name="action"></param>
    public static void ElasticStackExceptionLogger(this Exception e, IHostEnvironment hostEnvironment, 
        IDateTime dateTime, ILogger logger, string service, string action
    )
    {
        try
        {
            var nowDateTime        = DateTime.Now;
            var nowPersianDateTime = dateTime.ToPersianShortDate(nowDateTime);
            
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
            
            logger.Error($"{service}-{action}:{systemException.Serialize()}");
        }
        catch (Exception exception)
        {
            exception.FileLogger(hostEnvironment, dateTime);
        }
    }
}