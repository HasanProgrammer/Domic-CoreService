using System.Reflection;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.WebAPI.Jobs;
using Domic.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.WebAPI.Extensions;

public static class WebApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceName"></param>
    public static void RegisterGrpcServer(this WebApplicationBuilder builder)
    {
        Type[] domainAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Domain")).GetTypes();
        
        var icommandUnitOfWorkType =
            domainAssemblyTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
            );

        builder.Services.AddGrpc(options => {
            
            if(icommandUnitOfWorkType is not null)
                options.Interceptors.Add<FullExceptionHandlerInterceptor>(
                    builder.Configuration, builder.Environment, icommandUnitOfWorkType
                );
            else
                options.Interceptors.Add<ExceptionHandlerInterceptor>(builder.Configuration, builder.Environment);

            options.EnableDetailedErrors = true;
            
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterJobs(this WebApplicationBuilder builder)
    {
        Type[] webApiAssemblyTypes = Assembly.Load(new AssemblyName("Domic.WebAPI")).GetTypes();

        var jobTypes = webApiAssemblyTypes.Where(type =>
            type.GetInterfaces().Any(i => i == typeof(IHostedService))
        );

        foreach (var jobType in jobTypes)
            builder.Services.AddHostedService(jobType);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterRefreshSecretKey(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<RefreshSecretKeyJob>();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterServices(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<ServiceRegisteryJob>();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventsPublisher(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<ProducerEventJob>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventsPublisherAsEventSourcing(this WebApplicationBuilder builder) 
        => builder.Services.AddHostedService<ProducerEventStoreJob>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventsSubscriber(this WebApplicationBuilder builder) 
        => builder.Services.AddHostedService<EventConsumerJob>();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterMessagesSubscriber(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<MessageConsumersJob>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterAsyncCommandsSubscriber(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<CommandConsumerJob>();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventsStreamPublisher(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<ProducerEventStreamJob>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventsStreamSubscriber(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<EventStreamConsumerJob>();
        builder.Services.AddHostedService<EventRetriableStreamConsumerJob>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterMessagesStreamSubscriber(this WebApplicationBuilder builder)
    {
        builder.Services.AddHostedService<MessageStreamConsumerJob>();
        builder.Services.AddHostedService<MessageRetriableStreamConsumerJob>();
    }
}