#pragma warning disable CS4014

using Grpc.Core;
using Grpc.Core.Interceptors;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Exceptions;
using Karami.Core.UseCase.Contracts.Interfaces;
using Karami.Core.UseCase.Exceptions;
using Karami.Core.UseCase.Extensions;
using Karami.Core.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

using ILogger = Serilog.ILogger;

namespace Karami.Core.WebAPI.Middlewares;

/// <summary>
/// For services that are Command base and can support Query Side .
/// </summary>
public class FullExceptionHandlerInterceptor : Interceptor
{
    private readonly Type             _icommandUnitOfWorkType;
    private readonly IConfiguration   _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    private IMessageBroker         _messageBroker;
    private IDateTime              _dateTime;
    private ILogger                _logger;
    private ICoreCommandUnitOfWork _commandUnitOfWork;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="service"></param>
    /// <param name="icommandUnitOfWorkType"></param>
    public FullExceptionHandlerInterceptor(IConfiguration configuration, IHostEnvironment hostEnvironment, 
        Type icommandUnitOfWorkType
    )
    {
        _configuration          = configuration;
        _hostEnvironment        = hostEnvironment;
        _icommandUnitOfWorkType = icommandUnitOfWorkType;
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
        var serviceName = _configuration.GetValue<string>("NameOfService");
        
        try
        {
            if(_icommandUnitOfWorkType is not null)
                _commandUnitOfWork =
                    context.GetHttpContext()
                           .RequestServices
                           .GetRequiredService(_icommandUnitOfWorkType) as ICoreCommandUnitOfWork;
            
            _dateTime      = context.GetHttpContext().RequestServices.GetRequiredService<IDateTime>();
            _logger        = context.GetHttpContext().RequestServices.GetRequiredService<ILogger>();
            _messageBroker = context.GetHttpContext().RequestServices.GetRequiredService<IMessageBroker>();
            
            context.CentralRequestLogger(_messageBroker, _dateTime, _hostEnvironment, serviceName, request);
            context.CheckLicense(_configuration);
            
            return await continuation(request, context);
        }
        catch (DomainException e) //For Command Side
        {
            _commandUnitOfWork.Rollback();
            
            var Response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
        catch (UseCaseException e) //For Command Side
        {
            _commandUnitOfWork.Rollback();
            
            var Response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
        catch (Exception e)
        {
            #region Logger

            e.FileLogger(_hostEnvironment, _dateTime);
            e.ElasticStackExceptionLogger(_hostEnvironment, _dateTime, _logger, serviceName, context.Method);
            e.CentralExceptionLogger(_hostEnvironment, _messageBroker, _dateTime, serviceName, context.Method);

            #endregion
         
            _commandUnitOfWork?.Rollback();

            var Response = new {
                Code    = _configuration.GetServerErrorStatusCode() ,
                Message = _configuration.GetServerErrorMessage()    ,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
    }
}