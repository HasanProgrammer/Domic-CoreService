#pragma warning disable CS4014

using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Exceptions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Domic.Core.Common.ClassExtensions;
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

    private IMessageBroker           _messageBroker;
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
        var serviceName = _configuration.GetValue<string>("NameOfService");
        
        try
        {
            if(_iCommandUnitOfWorkType is not null)
                _coreCommandUnitOfWork =
                    context.GetHttpContext()
                           .RequestServices
                           .GetRequiredService(_iCommandUnitOfWorkType) as ICoreCommandUnitOfWork;
            
            _dateTime                = context.GetHttpContext().RequestServices.GetRequiredService<IDateTime>();
            _messageBroker           = context.GetHttpContext().RequestServices.GetRequiredService<IMessageBroker>();
            _globalUniqueIdGenerator = context.GetHttpContext().RequestServices.GetRequiredService<IGlobalUniqueIdGenerator>();
            
            context.CentralRequestLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
                serviceName, request, context.CancellationToken
            );
            
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
            _coreCommandUnitOfWork.Rollback();
            
            var Response = new {
                Code    = _configuration.GetErrorStatusCode(),
                Message = e.Message,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
        catch (UseCaseException e) //For command side
        {
            _coreCommandUnitOfWork.Rollback();
            
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
            
            e.CentralExceptionLoggerAsync(_hostEnvironment, _globalUniqueIdGenerator, _messageBroker, _dateTime, 
                serviceName, context.Method, context.CancellationToken
            );

            #endregion
         
            _coreCommandUnitOfWork?.Rollback();

            var Response = new {
                Code    = _configuration.GetServerErrorStatusCode() ,
                Message = _configuration.GetServerErrorMessage()    ,
                Body    = new { }
            };

            throw new RpcException(new Status(StatusCode.Internal, JsonConvert.SerializeObject(Response)));
        }
    }
}