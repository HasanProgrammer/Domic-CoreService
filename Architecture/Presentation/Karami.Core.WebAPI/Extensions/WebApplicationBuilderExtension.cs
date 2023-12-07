using System.Reflection;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.WebAPI.Jobs;
using Karami.Core.WebAPI.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Karami.Core.WebAPI.Extensions;

public static class WebApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceName"></param>
    public static void RegisterGrpcServer(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration.GetValue<string>("NameOfService");
        
        Type[] domainAssemblyTypes = Assembly.Load(new AssemblyName("Karami.Domain")).GetTypes();
        
        var icommandUnitOfWorkType =
            domainAssemblyTypes.FirstOrDefault(type =>
                type.GetInterfaces().Any(i => i == typeof(ICoreCommandUnitOfWork))
            );

        builder.Services.AddGrpc(options => {
            
            if(icommandUnitOfWorkType is not null)
                options.Interceptors.Add<FullExceptionHandlerInterceptor>(
                    builder.Configuration, builder.Environment, serviceName, icommandUnitOfWorkType
                );
            else
                options.Interceptors.Add<ExceptionHandlerInterceptor>(
                    builder.Configuration, builder.Environment, serviceName
                );

            options.EnableDetailedErrors = true;
            
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterJobs(this WebApplicationBuilder builder)
    {
        Type[] webApiAssemblyTypes = Assembly.Load(new AssemblyName("Karami.WebAPI")).GetTypes();

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
    public static void RegisterEventsSubscriber(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<EventConsumerJob>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterAsyncCommandsSubscriber(this WebApplicationBuilder builder)
        => builder.Services.AddHostedService<CommandConsumerJob>();
}