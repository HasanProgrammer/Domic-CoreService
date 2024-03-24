#pragma warning disable CS4014

using Domic.Core.Common.ClassExceptions;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Exceptions;
using Domic.Core.WebAPI.Exceptions;
using Grpc.Core;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.Exceptions;
using Domic.Core.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Middlewares;

public class ExceptionHandler
{
    private readonly RequestDelegate _next;
    
    private IConfiguration           _configuration;
    private IHostEnvironment         _hostEnvironment;
    private IMessageBroker           _messageBroker;
    private IDateTime                _dateTime;
    private IGlobalUniqueIdGenerator _globalUniqueIdGenerator;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    public ExceptionHandler(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context)
    {
        var serviceName = _configuration.GetValue<string>("NameOfService");
        
        try
        {
            _configuration           = context.RequestServices.GetRequiredService<IConfiguration>();
            _hostEnvironment         = context.RequestServices.GetRequiredService<IHostEnvironment>();
            _dateTime                = context.RequestServices.GetRequiredService<IDateTime>();
            _messageBroker           = context.RequestServices.GetRequiredService<IMessageBroker>(); 
            _globalUniqueIdGenerator = context.RequestServices.GetRequiredService<IGlobalUniqueIdGenerator>();
            
            context.CentralRequestLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
                serviceName, default
            );

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
                
                var payload = _MainExceptionProcessing(context, e, serviceName);
            
                await context.JsonContent().StatusCode(200).SendPayloadAsync(payload);
            }
            else 
                await context.JsonContent().StatusCode(200).SendPayloadAsync(e.Status.Detail);
        }
        catch (Exception e)
        {
            var payload = _MainExceptionProcessing(context, e, serviceName);
            
            await context.JsonContent().StatusCode(200).SendPayloadAsync(payload);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    private object _MainExceptionProcessing(HttpContext context, Exception exception, string serviceName)
    {
        #region Logger

        exception.FileLogger(_hostEnvironment, _dateTime);
        
        exception.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
            serviceName, context.Request.Path
        );
        
        exception.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
            serviceName, context.Request.Path, default
        );

        #endregion
            
        return new {
            code = _configuration.GetServerErrorStatusCode() ,
            msg  = _configuration.GetServerErrorMessage()    ,
            body = new { }
        };
    }
}