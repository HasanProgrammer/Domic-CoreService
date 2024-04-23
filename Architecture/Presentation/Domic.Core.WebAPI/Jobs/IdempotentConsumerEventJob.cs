#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using System.Reflection;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Domain.Enumerations;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Attributes;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace Domic.Core.WebAPI.Jobs;

public class IdempotentConsumerEventJob : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMessageBroker _messageBroker;

    private Timer _timer;

    public IdempotentConsumerEventJob(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration,
        IHostEnvironment hostEnvironment, IMessageBroker messageBroker
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _messageBroker = messageBroker;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(state => _WorkerAsync(cancellationToken), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0); //Reset
        
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
    
    /*---------------------------------------------------------------*/

    private async Task _WorkerAsync(CancellationToken cancellationToken)
    {
        var nameOfAction  = nameof(IdempotentConsumerEventJob);
        var nameOfService = _configuration.GetValue<string>("NameOfService");
        
        using var scope = _serviceScopeFactory.CreateScope();

        var dateTime = scope.ServiceProvider.GetRequiredService<IDateTime>();
        var globalUniqueIdGenerator = scope.ServiceProvider.GetRequiredService<IGlobalUniqueIdGenerator>();
        var redisCache = scope.ServiceProvider.GetRequiredService<IRedisCache>();

        var idempotentRepository =
            scope.ServiceProvider.GetRequiredService<IIdempotentConsumerEventQueryRepository>();
        
        var queryUnitOfWork =
            scope.ServiceProvider.GetRequiredService(_GetTypeOfQueryUnitOfWork()) as ICoreQueryUnitOfWork;

        try
        {
            var events = await idempotentRepository.FindAllAsync(cancellationToken);
            
            #region RemoveDuplicateAndInActiveEvents

            //remove duplicate events ( based on event id )
        
            queryUnitOfWork.Transaction();
        
            idempotentRepository.RemoveRange(
                events.GroupBy(@event => @event.Id)
                      .Where(grouped => grouped.Count() > 1)
                      .SelectMany(projection => projection.Skip(1))
            );
            
            //remove inactive events
            
            idempotentRepository.RemoveRange( events.Where(@event => @event.IsActive == IsActive.InActive) );
            
            queryUnitOfWork.Commit();

            #endregion
            
            #region CallEventConsumerHandler
            
            var useCaseTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
            
            foreach (var @event in events)
            {
                queryUnitOfWork.Transaction();
                
                var targetConsumerEventBusHandlerType = useCaseTypes.FirstOrDefault(
                    type => type.GetInterfaces().Any(
                        i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IConsumerEventBusHandler<>) &&
                             i.GetGenericArguments().Any(arg => arg.Name.Equals(@event.Type))
                    )
                );

                if (targetConsumerEventBusHandlerType is not null)
                {
                    var eventType = targetConsumerEventBusHandlerType.GetInterfaces()
                                                                     .Select(i => i.GetGenericArguments()[0])
                                                                     .FirstOrDefault();
            
                    var fullContractOfConsumerType = typeof(IConsumerEventBusHandler<>).MakeGenericType(eventType);
                
                    var eventBusHandler = scope.ServiceProvider.GetRequiredService(fullContractOfConsumerType);
                    
                    var eventBusHandlerType = eventBusHandler.GetType();
                    
                    var payload = JsonConvert.DeserializeObject(@event.Payload, eventType);

                    MethodInfo eventBusHandlerMethod;
                    
                    if (_configuration.GetValue<bool>("IsExternalBrokerConsumingAsync"))
                    {
                        eventBusHandlerMethod =
                            eventBusHandlerType.GetMethod("HandleAsync") ?? throw new Exception("HandleAsync function not found !");
                    
                        await (Task)eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload , cancellationToken });
                    }
                    else
                    {
                        eventBusHandlerMethod =
                            eventBusHandlerType.GetMethod("Handle") ?? throw new Exception("Handle function not found !");
                    
                        eventBusHandlerMethod.Invoke(eventBusHandler, new[] { payload });
                    }

                    var nowDateTime = DateTime.Now;

                    @event.IsActive = IsActive.InActive;
                    @event.UpdatedAt_EnglishDate = nowDateTime;
                    @event.UpdatedAt_PersianDate = dateTime.ToPersianShortDate(nowDateTime);

                    _CleanCache(eventBusHandlerMethod, redisCache, dateTime, globalUniqueIdGenerator, nameOfService, 
                        nameOfAction
                    );
                }
                
                queryUnitOfWork.Commit();
            }

            #endregion
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, dateTime);
                
            e.ElasticStackExceptionLogger(_hostEnvironment, globalUniqueIdGenerator, dateTime, 
                nameOfService, nameOfAction
            );
                
            e.CentralExceptionLogger(_hostEnvironment, globalUniqueIdGenerator, _messageBroker, dateTime, nameOfService, 
                nameOfAction
            );
            
            queryUnitOfWork?.Dispose();
        }
    }
    
    private Type _GetTypeOfQueryUnitOfWork()
    {
        var domainTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();

        return domainTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i == typeof(ICoreQueryUnitOfWork))
        );
    }
    
    private void _CleanCache(MethodInfo eventBusHandlerMethod, IRedisCache redisCache, IDateTime dateTime,
        IGlobalUniqueIdGenerator globalUniqueIdGenerator, string nameOfService, string nameOfAction
    )
    {
        try
        {
            if (eventBusHandlerMethod.GetCustomAttribute(typeof(WithCleanCacheAttribute)) is WithCleanCacheAttribute withCleanCacheAttribute)
                foreach (var key in withCleanCacheAttribute.Keies.Split("|"))
                    redisCache.DeleteKey(key);
        }
        catch (Exception e)
        {
            e.FileLogger(_hostEnvironment, dateTime);
            
            e.ElasticStackExceptionLogger(_hostEnvironment, globalUniqueIdGenerator, dateTime, 
                nameOfService, nameOfAction
            );
        }
    }
}