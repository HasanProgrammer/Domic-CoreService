﻿using System.Reflection;
using System.Text;
using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Attributes;
using Domic.Core.Infrastructure.Concretes;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Hangfire;
using Hangfire.SqlServer;
using Domic.Core.Common.ClassConsts;
using Domic.Core.Common.ClassExceptions;
using Domic.Core.Common.ClassExtensions;
using Domic.Core.Persistence.Contexts;
using Domic.Core.Persistence.Interceptors;
using Domic.Core.Service.Grpc;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Nest;
using RabbitMQ.Client;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

using ILogger     = Domic.Core.UseCase.Contracts.Interfaces.ILogger;
using Environment = System.Environment;

namespace Domic.Core.Infrastructure.Extensions;

public static class WebApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterHelpers(this WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton(typeof(IMemoryCacheReflectionAssemblyType), typeof(MemoryCacheReflectionAssemblyType));
        
        builder.Services.AddSingleton(typeof(IDateTime), typeof(DomicDateTime));
        
        builder.Services.AddSingleton(typeof(ISerializer), typeof(Serializer));
        
        builder.Services.AddSingleton(typeof(IJsonWebToken), typeof(JsonWebToken));
        
        builder.Services.AddSingleton(typeof(IGlobalUniqueIdGenerator), typeof(GlobalUniqueIdGenerator));
        
        builder.Services.AddKeyedScoped(typeof(IIdentityUser), "Http1", typeof(IdentityUser));
        builder.Services.AddKeyedScoped(typeof(IIdentityUser), "Http2", typeof(Http2IdentityUser));
        
        builder.Services.AddScoped(typeof(ILogger), typeof(Logger));
        
        builder.Services.AddScoped(typeof(IStreamLogger), typeof(StreamLogger));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterELK(this WebApplicationBuilder builder)
    {
        var elasticUri      = Environment.GetEnvironmentVariable("Elastic-Host");
        var elasticIndex    = Environment.GetEnvironmentVariable("Elastic-Index");
        var elasticUsername = Environment.GetEnvironmentVariable("Elastic-Username");
        var elasticPassword = Environment.GetEnvironmentVariable("Elastic-Password");
        
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false,
                                                          reloadOnChange: true
                                                      )
                                                      .AddJsonFile($"appsettings.{environment}.json", optional: true)
                                                      .Build();
        
        #region Elastic

        var settings = new ConnectionSettings(new Uri(elasticUri));

        settings.BasicAuthentication(elasticUsername, elasticPassword);

        builder.Services.AddSingleton<IElasticClient>(new ElasticClient(settings));

        #endregion

        #region Serilog&Kibana

        var indexOfEnvironment = environment?.ToLower().Replace(".", "-");
        
        var elOptions = new ElasticsearchSinkOptions( new Uri(elasticUri) ) {
            AutoRegisterTemplate = true,
            IndexFormat = $"{elasticIndex}-{indexOfEnvironment}-{DateTime.UtcNow:yyyy-MM}"
        };

        Log.Logger = new LoggerConfiguration().Enrich.FromLogContext()
                                              .Enrich.WithMachineName()
                                              .WriteTo.Debug()
                                              .WriteTo.Console()
                                              .WriteTo.Elasticsearch(elOptions)
                                              .Enrich.WithProperty("Environment", environment)
                                              .ReadFrom.Configuration(configuration)
                                              .CreateLogger();

        builder.Host.UseSerilog();

        #endregion
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TIdentity"></typeparam>
    public static void RegisterEntityFrameworkCoreCommand<TContext, TIdentity>(this WebApplicationBuilder builder) 
        where TContext : DbContext
    {
        if(builder.Environment.EnvironmentName.Equals(Domic.Core.Common.ClassConsts.Environment.Testing))
            builder.Services.AddDbContext<TContext>(config => config.UseInMemoryDatabase("Testing-CommandDatabase"));
        else
        {
            builder.Services.AddScoped<EfOutBoxPublishEventInterceptor<TIdentity>>();
            
            builder.Services.AddDbContext<TContext>((provider, config) => 
                config.UseSqlServer(builder.Configuration.GetCommandSqlServerConnectionString())
                      .AddInterceptors(provider.GetRequiredService<EfOutBoxPublishEventInterceptor<TIdentity>>())
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TContext"></typeparam>
    public static void RegisterEntityFrameworkCoreQuery<TContext>(this WebApplicationBuilder builder) 
        where TContext : DbContext
    {
        if(builder.Environment.EnvironmentName.Equals(Domic.Core.Common.ClassConsts.Environment.Testing))
            builder.Services.AddDbContext<TContext>(config => config.UseInMemoryDatabase("Testing-QueryDatabase"));
        else
            builder.Services.AddDbContext<TContext>(config =>
                config.UseSqlServer(builder.Configuration.GetQuerySqlServerConnectionString())
            );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterMongoDbDriver(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<MongoClient>(Provider => 
            new MongoClient(
                builder.Configuration.GetMongoConnectionString()
            )
        );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventSourcing(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        
        builder.Services.AddDbContext<EventStoreContext>((provider, config) => 
            config.UseNpgsql(builder.Configuration.GetEventStoreConnectionString())
        );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static void RegisterDistributedCaching(this WebApplicationBuilder builder)
    {
        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        //Third party ( Redis )
        builder.Services.AddKeyedScoped<IConnectionMultiplexer>("InternalRedis",
            (_p, _o) => ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("I-RedisConnectionString"))
        );
        
        //Third party ( Redis )
        builder.Services.AddKeyedScoped<IConnectionMultiplexer>("ExternalRedis",
            (_p, _o) => ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("E-RedisConnectionString"))
        );
        
        //Pure
        builder.Services.AddScoped(typeof(IInternalDistributedCache), typeof(InternalDistributedCache));
        builder.Services.AddScoped(typeof(IExternalDistributedCache), typeof(ExternalDistributedCache));
        
        //Pure ( Mediator for cache )
        builder.Services.AddTransient(
            typeof(IInternalDistributedCacheMediator), typeof(InternalDistributedCacheMediator)
        );
        
        builder.Services.AddTransient(
            typeof(IExternalDistributedCacheMediator), typeof(ExternalDistributedCacheMediator)
        );
        
        RegisterAllInternalDistributedCachesHandler(builder.Services, useCaseAssemblyTypes);
        RegisterAllExternalDistributedCachesHandler(builder.Services, useCaseAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterElmahLogger(this WebApplicationBuilder builder)
    {
        builder.Services.AddElmahIo(configs => {
            configs.ApiKey = "4d9df258aa614420b9fdb1301ff24127";
            configs.LogId  = new Guid("85c2f56c-9cf2-427a-a1f3-0aeffe01cffe");
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterHangfire(this WebApplicationBuilder builder)
    {
        builder.Services.AddHangfire(
            config => config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                            .UseSimpleAssemblyNameTypeSerializer()
                            .UseRecommendedSerializerSettings()
                            .UseSqlServerStorage(
                                builder.Configuration.GetCommandSqlServerConnectionString(),
                                new SqlServerStorageOptions {
                                    CommandBatchMaxTimeout       = TimeSpan.FromMinutes(5) ,
                                    SlidingInvisibilityTimeout   = TimeSpan.FromMinutes(5) ,
                                    QueuePollInterval            = TimeSpan.Zero           ,
                                    UseRecommendedIsolationLevel = true                    ,
                                    DisableGlobalLocks           = true
                                }
                            )
        );

        builder.Services.AddHangfireServer();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <exception cref="TokenNotValidException"></exception>
    /// <exception cref="TokenExpireException"></exception>
    /// <exception cref="UnAuthorizedException"></exception>
    /// <exception cref="ChallengeException"></exception>
    public static void RegisterJsonWebToken(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(options => {
                
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            
        })
        .AddJwtBearer(config => {
            
            config.TokenValidationParameters = new TokenValidationParameters {
                ValidIssuer              = builder.Configuration.GetValue<string>("JWT:Issuer"),
                ValidAudience            = builder.Configuration.GetValue<string>("JWT:Audience"),
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes( builder.Configuration.GetValue<string>("JWT:Key") )),
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime         = true
            };

            config.Events = new JwtBearerEvents {
                
                OnMessageReceived = context => {

                    #region SignalR's Socket

                    string Token    = context.Request.Query["access_token"];
                    PathString Path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(Token) && Path.StartsWithSegments("/chat"))
                        context.Token = Token;

                    #endregion
                
                    return Task.CompletedTask;
                    
                }
                    
                ,
                
                OnAuthenticationFailed = context => {
                    
                    if (context.Exception.GetType() == typeof(SecurityTokenSignatureKeyNotFoundException)) 
                        throw new TokenNotValidException();
                    
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException)) 
                        throw new TokenExpireException();

                    return Task.CompletedTask;
                    
                }
                
                ,
                
                OnForbidden = context => throw new UnAuthorizedException()
                
                ,
                
                OnChallenge = context => {
                    
                    if (context.AuthenticateFailure != null)
                        throw new ChallengeException();

                    return Task.CompletedTask;
                    
                }
            };
            
        });
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterServiceDiscovery(this WebApplicationBuilder builder)
    {
        Type[] coreUseCaseAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Core.UseCase")).GetTypes();
        
        RegisterAllExternalDistributedCachesHandler(builder.Services, coreUseCaseAssemblyTypes);
        
        builder.Services.AddScoped(typeof(IServiceDiscovery), typeof(ServiceDiscovery));

        builder.Services.AddGrpcClient<DiscoveryService.DiscoveryServiceClient>(options => {
                
            options.Address = new Uri(Environment.GetEnvironmentVariable("ServiceDiscoveryAddress"));

        })
        .AddCallCredentials((context, metadata, serviceProvider) => {
            
            metadata.Add(Header.License, 
                serviceProvider.GetRequiredService<IExternalDistributedCache>().GetCacheValue("SecretKey")
            );

            return Task.CompletedTask;

        })
        .ConfigureChannel(options => {
            
            var methodConfig = new MethodConfig {
                RetryPolicy = new RetryPolicy {
                    MaxAttempts          = 5, //count retry
                    InitialBackoff       = TimeSpan.FromSeconds(1), //start waiting time
                    MaxBackoff           = TimeSpan.FromSeconds(5), //max waiting time
                    BackoffMultiplier    = 2, //waiting time between each attempt
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            };

            options.HttpHandler = new HttpClientHandler {
                ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            
            var serviceConfig = new ServiceConfig {
                MethodConfigs = { methodConfig }, //retry
            };

            options.ServiceConfig = serviceConfig;

        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterServicesOfGrpcClientWebRequest(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Infrastructure")).GetTypes();
        
        RegisterAllServicesOfGrpcClientWebRequest(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterServicesOfHttpWebRequest(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Infrastructure")).GetTypes();
        
        RegisterAllServicesOfHttpWebRequest(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterCommandRepositories(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Infrastructure")).GetTypes();
        
        RegisterAllCommandRepositories(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterQueryRepositories(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Domic.Infrastructure")).GetTypes();
        
        RegisterAllQueryRepositories(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="PoolChannelCapacity"></param>
    public static void RegisterCommandQueryUseCases(this WebApplicationBuilder builder, int PoolChannelCapacity = 500)
    {
        builder.Services.AddKeyedSingleton<IConnection>("Internal", (provider, _) => {
            
            var configuration = provider.GetService<IConfiguration>();
            
            var factory = new ConnectionFactory {
                HostName = configuration.GetExternalRabbitHostName(),
                UserName = configuration.GetExternalRabbitUsername(),
                Password = configuration.GetExternalRabbitPassword(),
                Port     = configuration.GetExternalRabbitPort() 
            };

            factory.DispatchConsumersAsync = configuration.GetValue<bool>("IsInternalBrokerConsumingAsync");
        
            return factory.CreateConnection();
            
        });
        
        builder.Services.AddKeyedSingleton<IPooledObjectPolicy<IModel>, InternalChannelObjectPoolPolicy>("Internal");

        builder.Services.AddKeyedSingleton<ObjectPool<IModel>>("Internal", (provider, _) => {

            var objectPoolProvider = new DefaultObjectPoolProvider { MaximumRetained = PoolChannelCapacity };
            
            var policy = provider.GetRequiredKeyedService<IPooledObjectPolicy<IModel>>("Internal");
            
            return objectPoolProvider.Create(policy);
            
        });
        
        builder.Services.AddTransient(typeof(IMediator), typeof(Mediator));
        builder.Services.AddSingleton(typeof(IInternalMessageBroker), typeof(InternalMessageBroker));
        
        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        RegisterAllQueriesHandler(builder.Services, useCaseAssemblyTypes);
        RegisterAllCommandsHandler(builder.Services, useCaseAssemblyTypes);
        RegisterAllAsyncCommandsHandler(builder.Services, useCaseAssemblyTypes);
        RegisterAllValidators(builder.Services, useCaseAssemblyTypes);
        RegisterAllAsyncValidators(builder.Services, useCaseAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="PoolChannelCapacity"></param>
    public static void RegisterMessageBroker(this WebApplicationBuilder builder, int PoolChannelCapacity = 500)
    {
        builder.Services.AddKeyedSingleton<IConnection>("External", (provider, _) => {
            
            var configuration = provider.GetService<IConfiguration>();
            
            var factory = new ConnectionFactory {
                HostName = configuration.GetExternalRabbitHostName(),
                UserName = configuration.GetExternalRabbitUsername(),
                Password = configuration.GetExternalRabbitPassword(),
                Port     = configuration.GetExternalRabbitPort() 
            };

            factory.DispatchConsumersAsync = configuration.GetValue<bool>("IsExternalBrokerConsumingAsync");
        
            return factory.CreateConnection();
            
        });
        
        builder.Services.AddKeyedSingleton<IPooledObjectPolicy<IModel>, ExternalChannelObjectPoolPolicy>("External");

        builder.Services.AddKeyedSingleton<ObjectPool<IModel>>("External", (provider, _) => {

            var objectPoolProvider = new DefaultObjectPoolProvider { MaximumRetained = PoolChannelCapacity };
            
            var policy = provider.GetRequiredKeyedService<IPooledObjectPolicy<IModel>>("External");
            
            return objectPoolProvider.Create(policy);
            
        });
        
        builder.Services.AddSingleton(typeof(IExternalMessageBroker), typeof(ExternalMessageBroker));

        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        RegisterAllConsumerMessageBusHandlers(builder.Services, useCaseAssemblyTypes);
        RegisterAllConsumerEventBusHandler(builder.Services, useCaseAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterEventStreamBroker(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(typeof(IExternalEventStreamBroker), typeof(ExternalEventStreamBroker));

        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Domic.UseCase")).GetTypes();
        
        RegisterAllConsumerEventStreamHandler(builder.Services, useCaseAssemblyTypes);
        RegisterAllConsumerMessageStreamHandler(builder.Services, useCaseAssemblyTypes);
    }
    
    /*---------------------------------------------------------------*/

    #region AutoRegisterCQRSHandlers
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllCommandsHandler(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        IEnumerable<Type> commandHandlerTypes = useCaseAssemblyTypes.Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)));
        
        foreach (Type commandHandlerType in commandHandlerTypes) {
                
            Type[] icommandHandlerTypeValues = commandHandlerType.GetInterfaces()?.FirstOrDefault()?.GetGenericArguments();
            
            serviceCollection.AddTransient(
                typeof(ICommandHandler<,>).MakeGenericType( icommandHandlerTypeValues[0] , icommandHandlerTypeValues[1] ),
                commandHandlerType
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllAsyncCommandsHandler(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        var asynCommandHandlerTypes =
            useCaseAssemblyTypes.Where(type => type.GetInterfaces().Any(i => 
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerCommandBusHandler<,>)
                )
            );
        
        foreach (Type asyncCommandHandlerType in asynCommandHandlerTypes) {
                
            Type[] icommandHandlerTypeValues = 
                asyncCommandHandlerType.GetInterfaces()?.FirstOrDefault()?.GetGenericArguments();
            
            serviceCollection.AddTransient(
                typeof(IConsumerCommandBusHandler<,>).MakeGenericType( icommandHandlerTypeValues[0] , icommandHandlerTypeValues[1] ),
                asyncCommandHandlerType
            );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllQueriesHandler(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        IEnumerable<Type> queryHandlerTypes = useCaseAssemblyTypes.Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));
        
        foreach (Type queryHandlerType in queryHandlerTypes) {
                
            Type[] iqueryHandlerTypeValues = queryHandlerType.GetInterfaces()?.FirstOrDefault()?.GetGenericArguments();
            
            serviceCollection.AddTransient(
                typeof(IQueryHandler<,>).MakeGenericType( iqueryHandlerTypeValues[0] , iqueryHandlerTypeValues[1] ),
                queryHandlerType
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllValidators(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        IEnumerable<Type> validatorsTypes = useCaseAssemblyTypes.Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)));
        
        foreach (Type validatorType in validatorsTypes) {
                
            Type[] ivalidatorTypeValues = validatorType.GetInterfaces()?.FirstOrDefault()?.GetGenericArguments();
            
            serviceCollection.AddTransient(
                typeof(IValidator<>).MakeGenericType( ivalidatorTypeValues[0] ),
                validatorType
            );
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllAsyncValidators(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        IEnumerable<Type> validatorsTypes = 
            useCaseAssemblyTypes.Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncValidator<>)));
        
        foreach (Type validatorType in validatorsTypes) {
                
            Type[] ivalidatorTypeValues = validatorType.GetInterfaces()?.FirstOrDefault()?.GetGenericArguments();
            
            serviceCollection.AddTransient(
                typeof(IAsyncValidator<>).MakeGenericType( ivalidatorTypeValues[0] ),
                validatorType
            );
        }
    }
    
    #endregion
    
    #region AutoRegisterMessageBroker

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllConsumerMessageBusHandlers(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        IEnumerable<Type> messageHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerMessageBusHandler<>)
            )
        );

        foreach (Type messageHandlerType in messageHandlerTypes) {
                
            Type messageHandlerTypeValue = 
                messageHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddScoped(
                typeof(IConsumerMessageBusHandler<>).MakeGenericType(messageHandlerTypeValue),
                messageHandlerType
            );
                
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllConsumerEventBusHandler(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        IEnumerable<Type> eventHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerEventBusHandler<>)
            )
        );

        foreach (Type eventHandlerType in eventHandlerTypes) {
                
            Type eventHandlerTypeValue = 
                eventHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddScoped(
                typeof(IConsumerEventBusHandler<>).MakeGenericType(eventHandlerTypeValue),
                eventHandlerType
            );
                
        }
    }

    #endregion

    #region AutoRegisterEventStreamBroker
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllConsumerEventStreamHandler(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        IEnumerable<Type> eventStreamHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerEventStreamHandler<>)
            )
        );

        foreach (Type eventStreamHandlerType in eventStreamHandlerTypes) {
                
            Type eventStreamHandlerTypeValue =
                eventStreamHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddScoped(
                typeof(IConsumerEventStreamHandler<>).MakeGenericType(eventStreamHandlerTypeValue),
                eventStreamHandlerType
            );
                
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllConsumerMessageStreamHandler(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        IEnumerable<Type> messageStreamHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumerMessageStreamHandler<>)
            )
        );

        foreach (Type messageStreamHandlerType in messageStreamHandlerTypes) {
                
            Type messageStreamHandlerTypeValue =
                messageStreamHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddScoped(
                typeof(IConsumerMessageStreamHandler<>).MakeGenericType(messageStreamHandlerTypeValue),
                messageStreamHandlerType
            );
                
        }
    }

    #endregion

    #region AutoRegisterCacheHandler

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllInternalDistributedCachesHandler(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        var cacheHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInternalDistributedCacheHandler<>)
            )
        );

        foreach (Type cacheHandlerType in cacheHandlerTypes) {
                
            Type cacheHandlerTypeValue =
                cacheHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddTransient(
                typeof(IInternalDistributedCacheHandler<>).MakeGenericType(cacheHandlerTypeValue),
                cacheHandlerType
            );
            
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllExternalDistributedCachesHandler(IServiceCollection serviceCollection, 
        Type[] useCaseAssemblyTypes
    )
    {
        var cacheHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExternalDistributedCacheHandler<>)
            )
        );

        foreach (Type cacheHandlerType in cacheHandlerTypes) {
                
            Type cacheHandlerTypeValue =
                cacheHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddTransient(
                typeof(IExternalDistributedCacheHandler<>).MakeGenericType(cacheHandlerTypeValue),
                cacheHandlerType
            );
            
        }
    }

    #endregion

    #region AutoRegisterRepository

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="infrastructureAssemblyTypes"></param>
    private static void RegisterAllCommandRepositories(IServiceCollection serviceCollection, Type[] infrastructureAssemblyTypes)
    {
        Type commandUnitOfWorkType = infrastructureAssemblyTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i.GetInterfaces().Any(ii => ii == typeof(ICoreCommandUnitOfWork)))
        );

        Type contractCommandUnitOfWorkType = commandUnitOfWorkType.GetInterfaces().FirstOrDefault();

        if(commandUnitOfWorkType.GetCustomAttribute(typeof(OutdatedAttribute)) is null)
            serviceCollection.AddScoped(contractCommandUnitOfWorkType, commandUnitOfWorkType);
        
        IEnumerable<Type> commandRepositoryTypes = infrastructureAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i =>
                i.GetInterfaces()
                 .Any(ii => 
                     ii.IsGenericType && 
                     ii.GetGenericTypeDefinition() == typeof(ICommandRepository<,>)
                 )
            )
        );

        foreach (Type commandRepositoryType in commandRepositoryTypes) {
            
            Type contractType = commandRepositoryType.GetInterfaces().FirstOrDefault();
            
            if(commandRepositoryType.GetCustomAttribute(typeof(OutdatedAttribute)) is null)
                serviceCollection.AddScoped(contractType, commandRepositoryType);
            
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="infrastructureAssemblyTypes"></param>
    private static void RegisterAllQueryRepositories(IServiceCollection serviceCollection, 
        Type[] infrastructureAssemblyTypes
    )
    {
        Type queryUnitOfWorkType = infrastructureAssemblyTypes.FirstOrDefault(
            type => type.GetInterfaces().Any(i => i.GetInterfaces().Any(ii => ii == typeof(ICoreQueryUnitOfWork)))
        );

        Type contractQueryUnitOfWorkType = queryUnitOfWorkType.GetInterfaces().FirstOrDefault();

        if(queryUnitOfWorkType.GetCustomAttribute(typeof(OutdatedAttribute)) is null)
            serviceCollection.AddScoped(contractQueryUnitOfWorkType, queryUnitOfWorkType);
        
        IEnumerable<Type> queryRepositoryTypes = infrastructureAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i =>
                i.GetInterfaces()
                 .Any(ii => 
                     ii.IsGenericType && 
                     ii.GetGenericTypeDefinition() == typeof(IQueryRepository<,>)
                 )
            )
        );

        foreach (Type queryRepositoryType in queryRepositoryTypes) {
            
            Type contractType = queryRepositoryType.GetInterfaces().FirstOrDefault();
            
            if(queryRepositoryType.GetCustomAttribute(typeof(OutdatedAttribute)) is null) 
                serviceCollection.AddScoped(contractType, queryRepositoryType);
            
        }
    }

    #endregion

    #region AutoRegisterServicesOfWebRequest

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="infrastructureAssemblyTypes"></param>
    private static void RegisterAllServicesOfGrpcClientWebRequest(IServiceCollection serviceCollection,
        Type[] infrastructureAssemblyTypes
    )
    {
        IEnumerable<Type> services = infrastructureAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i => i.GetInterfaces().Any(ii => ii == typeof(IRpcWebRequest)))
        );

        foreach (Type service in services) {
                
            Type contractServiceType = service.GetInterfaces().FirstOrDefault();
                
            serviceCollection.AddScoped(contractServiceType, service);
            
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="infrastructureAssemblyTypes"></param>
    private static void RegisterAllServicesOfHttpWebRequest(IServiceCollection serviceCollection,
        Type[] infrastructureAssemblyTypes
    )
    {
        IEnumerable<Type> services = infrastructureAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i => i.GetInterfaces().Any(ii => ii == typeof(IHttpWebRequest)))
        );

        foreach (Type service in services) {
                
            Type contractServiceType = service.GetInterfaces().FirstOrDefault();
                
            serviceCollection.AddScoped(contractServiceType, service);
            
        }
    }

    #endregion
}