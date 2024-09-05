#pragma warning disable CS4014

using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Common.ClassModels;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.Exceptions;
using Domic.Core.WebAPI.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Domic.Core.WebAPI.Middlewares;

/// <summary>
/// For services that are Command base and can support Query Side .
/// </summary>
public class FullExceptionHandlerInterceptor : Interceptor
{
    private readonly Type             _iCommandUnitOfWorkType;
    private readonly IConfiguration   _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    private IExternalMessageBroker           _externalMessageBroker;
    private IExternalEventStreamBroker       _externalEventStreamBroker;
    private IDateTime                _dateTime;
    private ICoreCommandUnitOfWork   _coreCommandUnitOfWork;
    private IGlobalUniqueIdGenerator _globalUniqueIdGenerator;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <param name="service"></param>
    /// <param name="iCommandUnitOfWorkType"></param>
    public FullExceptionHandlerInterceptor(IConfiguration configuration, IHostEnvironment hostEnvironment, 
        Type iCommandUnitOfWorkType
    )
    {
        _configuration          = configuration;
        _hostEnvironment        = hostEnvironment;
        _iCommandUnitOfWorkType = iCommandUnitOfWorkType;
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
        var loggerType  = _configuration.GetSection("LoggerType").Get<LoggerType>();
        var serviceName = _configuration.GetValue<string>("NameOfService");
        
        try
        {
            if(_iCommandUnitOfWorkType is not null)
                _coreCommandUnitOfWork =
                    context.GetHttpContext()
                           .RequestServices
                           .GetRequiredService(_iCommandUnitOfWorkType) as ICoreCommandUnitOfWork;
            
            _dateTime                = context.GetHttpContext().RequestServices.GetRequiredService<IDateTime>();
            _globalUniqueIdGenerator = context.GetHttpContext().RequestServices.GetRequiredService<IGlobalUniqueIdGenerator>();

            if (loggerType.Messaging)
            {
                _externalMessageBroker = context.GetHttpContext().RequestServices.GetRequiredService<IExternalMessageBroker>();
                
                context.CentralRequestLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker, _dateTime, 
                    serviceName, request, context.CancellationToken
                );
            }
            else
            {
                _externalEventStreamBroker = context.GetHttpContext().RequestServices.GetRequiredService<IExternalEventStreamBroker>();
                
                context.CentralRequestLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalEventStreamBroker, 
                    _dateTime, serviceName, request, context.CancellationToken
                );
            }
            
            context.CheckLicense(_configuration);
            
            #region IdempotentReceiverPattern

            var idempotentKey = context.GetHttpContext().Request.Headers.IdempotentKey();

            if (!string.IsNullOrEmpty(idempotentKey))
            {
                var redisCache = context.GetHttpContext().RequestServices.GetRequiredService<IInternalDistributedCache>();
                var serializer = context.GetHttpContext().RequestServices.GetRequiredService<ISerializer>();
                
                var idempotentResponse =
                    await redisCache.GetCacheValueAsync($"RequestId-{idempotentKey}", context.CancellationToken);

                if (!string.IsNullOrEmpty(idempotentResponse))
                    return serializer.DeSerialize<TResponse>(idempotentResponse);
                
                var response = await continuation(request, context);

                await redisCache.SetCacheValueAsync(
                    new KeyValuePair<string, string>($"RequestId-{idempotentKey}", serializer.Serialize(response)),
                    time: TimeSpan.FromMinutes(5),
                    cancellationToken: context.CancellationToken
                );
            
                return response;
            }
                
            #endregion
            
            return await continuation(request, context);
        }
        catch (DomainException e) //For command side
        {
            await _RollbackAsync(context.CancellationToken);
            
            var Response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
        catch (UseCaseException e) //For command side
        {
            await _RollbackAsync(context.CancellationToken);
            
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
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, serviceName, 
                context.Method
            );

            if (_externalMessageBroker is not null)
            {
                e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker, _dateTime, 
                    serviceName, context.Method, context.CancellationToken
                );
            }
            else
            {
                e.CentralExceptionLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalEventStreamBroker, 
                    _dateTime, serviceName, context.Method, context.CancellationToken
                );
            }

            #endregion

            await _RollbackAsync(context.CancellationToken);

            var Response = new {
                Code    = _configuration.GetServerErrorStatusCode() ,
                Message = _configuration.GetServerErrorMessage()    ,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
    }

    private Task _RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            _coreCommandUnitOfWork?.Rollback();
        }
        catch (Exception e)
        {
            if(_coreCommandUnitOfWork is not null)
                return _coreCommandUnitOfWork.RollbackAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }
}