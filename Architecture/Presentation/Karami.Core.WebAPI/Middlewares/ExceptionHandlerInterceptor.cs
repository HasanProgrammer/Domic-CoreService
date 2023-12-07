#pragma warning disable CS4014

using Grpc.Core;
using Grpc.Core.Interceptors;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.Exceptions;
using Karami.Core.UseCase.Extensions;
using Karami.Core.WebAPI.Exceptions;
using Karami.Core.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Karami.Core.WebAPI.Middlewares;

/// <summary>
/// For services that only support Query Side and Command is not supposed to happen in them .
/// </summary>
public class ExceptionHandlerInterceptor : Interceptor
{
    private readonly string           _service;
    private readonly IConfiguration   _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    private IMessageBroker  _messageBroker;
    private IDotrisDateTime _dotrisDateTime;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="service"></param>
    /// <param name="icommandUnitOfWorkType"></param>
    public ExceptionHandlerInterceptor(IConfiguration configuration, IHostEnvironment hostEnvironment, string service)
    {
        _service         = service;
        _configuration   = configuration;
        _hostEnvironment = hostEnvironment;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <param name="continuation"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    /// <exception cref="RpcException"></exception>
    public override async Task<TResponse> UnaryServerHandler<TRequest , TResponse>(TRequest request , 
        ServerCallContext context , UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            _messageBroker  = context.GetHttpContext().RequestServices.GetRequiredService<IMessageBroker>();
            _dotrisDateTime = context.GetHttpContext().RequestServices.GetRequiredService<IDotrisDateTime>();
            
            context.CentralRequestLogger(_messageBroker, _dotrisDateTime, _hostEnvironment, _service, request);
            context.CheckLicense(_configuration);
            
            return await continuation(request, context);
        }
        catch (UseCaseException e)
        {
            var response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(response)));
        }
        catch (PresentationException e)
        {
            var response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(response)));
        }
        catch (Exception e)
        {
            #region Logger

            e.FileLogger(_hostEnvironment, _dotrisDateTime);
            e.CentralExceptionLogger(_hostEnvironment, _messageBroker, _dotrisDateTime, _service, context.Method);

            #endregion

            var response = new {
                Code    = _configuration.GetServerErrorStatusCode() ,
                Message = _configuration.GetServerErrorMessage()    ,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(response)));
        }
    }
}