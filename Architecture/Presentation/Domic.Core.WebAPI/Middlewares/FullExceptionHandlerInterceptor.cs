#pragma warning disable CS4014

using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Common.ClassModels;
using Domic.Core.Infrastructure.Concretes;
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
    private readonly LoggerType       _loggerType;

    private IExternalMessageBroker     _externalMessageBroker;
    private IExternalEventStreamBroker _externalEventStreamBroker;
    private IDateTime                  _dateTime;
    private ICoreCommandUnitOfWork     _coreCommandUnitOfWork;
    private IGlobalUniqueIdGenerator   _globalUniqueIdGenerator;
    
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
        _loggerType             = configuration.GetSection("LoggerType").Get<LoggerType>();
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
            var httpContext = context.GetHttpContext();
            
            var services = httpContext.RequestServices;
            
            var identityUser = services.GetRequiredKeyedService<IIdentityUser>("Http2") as Http2IdentityUser;
            
            identityUser.SetAuthToken(httpContext.GetTokenOfGrpcHeader());
            
            if(_iCommandUnitOfWorkType is not null)
                _coreCommandUnitOfWork = services.GetRequiredService(_iCommandUnitOfWorkType) as ICoreCommandUnitOfWork;
            
            _dateTime                = services.GetRequiredService<IDateTime>();
            _globalUniqueIdGenerator = services.GetRequiredService<IGlobalUniqueIdGenerator>();

            if (_loggerType.Messaging)
            {
                _externalMessageBroker = services.GetRequiredService<IExternalMessageBroker>();
                
                //fire&forget
                context.CentralRequestLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker,
                    _dateTime, serviceName, request, context.CancellationToken
                );
            }
            else
            {
                _externalEventStreamBroker = services.GetRequiredService<IExternalEventStreamBroker>();
                
                //fire&forget
                context.CentralRequestLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, 
                    _externalEventStreamBroker, _dateTime, serviceName, request, context.CancellationToken
                );
            }
            
            await context.CheckLicenseAsync(context.CancellationToken);
            
            #region IdempotentReceiverPattern

            var idempotentKey = httpContext.Request.Headers.IdempotentKey();

            if (!string.IsNullOrEmpty(idempotentKey))
            {
                var redisCache = services.GetRequiredService<IInternalDistributedCache>();
                var serializer = services.GetRequiredService<ISerializer>();
                
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

            //fire&forget
            e.FileLoggerAsync(_hostEnvironment, _dateTime, context.CancellationToken);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, _globalUniqueIdGenerator, _dateTime, serviceName, 
                context.Method
            );

            if (_loggerType.Messaging)
                //fire&forget
                e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _externalMessageBroker,
                    _dateTime, serviceName, context.Method, context.CancellationToken
                );
            else
                //fire&forget
                e.CentralExceptionLoggerAsStreamAsync(_hostEnvironment, _globalUniqueIdGenerator, 
                    _externalEventStreamBroker, _dateTime, serviceName, context.Method, context.CancellationToken
                );

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