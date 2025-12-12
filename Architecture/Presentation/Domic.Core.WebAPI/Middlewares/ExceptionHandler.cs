#pragma warning disable CS4014

using Domic.Core.Common.ClassExceptions;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Common.ClassModels;
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
    private readonly LoggerType _loggerType;
    
    private IConfiguration             _configuration;
    private IHostEnvironment           _hostEnvironment;
    private IExternalMessageBroker     _externalMessageBroker;
    private IExternalEventStreamBroker _externalEventStreamBroker;
    private IDateTime                  _dateTime;
    private IGlobalUniqueIdGenerator   _globalUniqueIdGenerator;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="next"></param>
    public ExceptionHandler(RequestDelegate next, IConfiguration configuration)
    {
        _next          = next;
        _configuration = configuration;
        _loggerType    = _configuration.GetSection("LoggerType").Get<LoggerType>();
    }

    public async Task Invoke(HttpContext context)
    {
        var serviceName = _configuration.GetValue<string>("NameOfService");
        
        try
        {
            _configuration           = context.RequestServices.GetRequiredService<IConfiguration>();
            _hostEnvironment         = context.RequestServices.GetRequiredService<IHostEnvironment>();
            _dateTime                = context.RequestServices.GetRequiredService<IDateTime>();
            _globalUniqueIdGenerator = context.RequestServices.GetRequiredService<IGlobalUniqueIdGenerator>();
            
            if (_loggerType.Messaging)
            {
                _externalMessageBroker = context.RequestServices.GetRequiredService<IExternalMessageBroker>();
                
                await context.CentralRequestLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker, 
                    _dateTime, serviceName, context.RequestAborted
                );
            }
            else
            {
                _externalEventStreamBroker = context.RequestServices.GetRequiredService<IExternalEventStreamBroker>();
                
                await context.CentralRequestLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, 
                    _externalEventStreamBroker, _dateTime, serviceName, context.RequestAborted
                );
            }

            await _next(context);
        }
        catch (TokenNotValidException)
        {
            var Payload = new {
                Code = _configuration.GetUnAuthorizedStatusCode(),
                Message = _configuration.GetTokenNotValidMessage(),
                Body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (TokenExpireException)
        {
            var Payload = new {
                Code = _configuration.GetUnAuthorizedStatusCode(),
                Message = _configuration.GetTokenExpireMessage(),
                Body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (ChallengeException)
        {
            var Payload = new {
                Code = _configuration.GetUnAuthorizedStatusCode(),
                Message = _configuration.GetChallengeMessage(),
                Body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (UnAuthorizedException)
        {
            var Payload = new {
                Code = _configuration.GetUnAuthorizedStatusCode(),
                Message = _configuration.GetUnAuthorizedMessage(),
                Body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (AuthenticationFailedException)
        {
            var Payload = new {
                Code = _configuration.GetUnAuthorizedStatusCode(),
                Message = _configuration.GetForbiddenMessage(),
                Body = new { }
            };

            await context.JsonContent().StatusCode(200).SendPayloadAsync(Payload);
        }
        catch (DomainException e)
        {
            await context.JsonContent().StatusCode(200).SendPayloadAsync(_GetLayerExceptionResponseDto(e.Message));
        }
        catch (UseCaseException e)
        {
            await context.JsonContent().StatusCode(200).SendPayloadAsync(_GetLayerExceptionResponseDto(e.Message));
        }
        catch (PresentationException e)
        {
            await context.JsonContent().StatusCode(200).SendPayloadAsync(_GetLayerExceptionResponseDto(e.Message));
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

        //fire&forget
        exception.FileLoggerAsync(_hostEnvironment, _dateTime, context.RequestAborted);
        
        exception.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, 
            serviceName, context.Request.Path
        );

        if (_loggerType.Messaging)
            //fire&forget
            exception.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker,
                _dateTime, serviceName, context.Request.Path, context.RequestAborted
            );
        else
            //fire&forget
            exception.CentralExceptionLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, 
                _externalEventStreamBroker, _dateTime, serviceName, context.Request.Path, context.RequestAborted
            );

        #endregion
            
        return new {
            Code = _configuration.GetServerErrorStatusCode(),
            Message = _configuration.GetServerErrorMessage(),
            Body = new { }
        };
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private object _GetLayerExceptionResponseDto(string message) => new {
        Code = _configuration.GetErrorStatusCode(),
        Message = message,
        Body = new { }
    };
}