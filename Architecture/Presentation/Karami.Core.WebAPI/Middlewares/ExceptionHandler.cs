#pragma warning disable CS4014

using Grpc.Core;
using Karami.Core.Common.ClassExceptions;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Exceptions;
using Karami.Core.Infrastructure.Extensions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.Exceptions;
using Karami.Core.UseCase.Extensions;
using Karami.Core.WebAPI.Exceptions;
using Karami.Core.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.WebAPI.Middlewares;

public class ExceptionHandler
{
    private readonly string          _serviceName;
    private readonly RequestDelegate _next;
    
    private IConfiguration   _configuration;
    private IHostEnvironment _hostEnvironment;
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="serviceName"></param>
    public ExceptionHandler(RequestDelegate next, string serviceName)
    {
        _next        = next;
        _serviceName = serviceName;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            _configuration     = context.RequestServices.GetRequiredService<IConfiguration>();
            _hostEnvironment   = context.RequestServices.GetRequiredService<IHostEnvironment>();
            var messageBroker  = context.RequestServices.GetRequiredService<IMessageBroker>();
            var dotrisDateTime = context.RequestServices.GetRequiredService<IDateTime>();

            context.CentralRequestLogger(_hostEnvironment, messageBroker, dotrisDateTime, _serviceName);

            await _next(context);
        }
        catch (TokenNotValidException)
        {
            var Payload = new {
                code = _configuration.GetUnAuthorizedStatusCode() ,
                msg  = _configuration.GetTokenNotValidMessage()   ,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (TokenExpireException)
        {
            var Payload = new {
                code = _configuration.GetUnAuthorizedStatusCode() ,
                msg  = _configuration.GetTokenExpireMessage()     ,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (ChallengeException)
        {
            var Payload = new {
                code = _configuration.GetUnAuthorizedStatusCode() ,
                msg  = _configuration.GetChallengeMessage()       ,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (UnAuthorizedException)
        {
            var Payload = new {
                code = _configuration.GetUnAuthorizedStatusCode(),
                msg  = _configuration.GetUnAuthorizedMessage(),
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (AuthenticationFailedException)
        {
            var Payload = new {
                code = _configuration.GetUnAuthorizedStatusCode() ,
                msg  = _configuration.GetForbiddenMessage()       ,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (DomainException e)
        {
            var Payload = new {
                code = _configuration.GetErrorStatusCode(),
                msg  = e.Message,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (UseCaseException e)
        {
            var Payload = new {
                code = _configuration.GetErrorStatusCode(),
                msg  = e.Message,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (PresentationException e)
        {
            var Payload = new {
                code = _configuration.GetErrorStatusCode(),
                msg  = e.Message,
                body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (RpcException e)
        {
            if (e.StatusCode == StatusCode.Unavailable)
            {
                //The target service does not accept the request
                
                var payload = _mainExceptionProcessing(context, e);
            
                await context.JsonContent().StatusCode(200).SendPayloadAsync(payload);
            }
            else 
                await context.JsonContent().StatusCode(200).SendPayloadAsync(e.Status.Detail);
        }
        catch (Exception e)
        {
            var payload = _mainExceptionProcessing(context, e);
            
            await context.JsonContent().StatusCode(200).SendPayloadAsync(payload);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    private object _mainExceptionProcessing(HttpContext context, Exception exception)
    {
        #region Logger

        var dotrisDateTime = context.RequestServices.GetService<IDateTime>();

        exception.FileLogger(_hostEnvironment, dotrisDateTime);
        exception.CentralExceptionLogger(_hostEnvironment, 
            context.RequestServices.GetService<IMessageBroker>(), dotrisDateTime, _serviceName, context.Request.Path
        );

        #endregion
            
        return new {
            code = _configuration.GetServerErrorStatusCode() ,
            msg  = _configuration.GetServerErrorMessage()    ,
            body = new { }
        };
    }
}