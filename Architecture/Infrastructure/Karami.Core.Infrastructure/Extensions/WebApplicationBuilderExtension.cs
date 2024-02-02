using System.Reflection;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Hangfire;
using Hangfire.SqlServer;
using Karami.Core.Common.ClassConsts;
using Karami.Core.Common.ClassExceptions;
using Karami.Core.Common.ClassExtensions;
using Karami.Core.Domain.Contracts.Interfaces;
using Karami.Core.Domain.Implementations;
using Karami.Core.Grpc.Service;
using Karami.Core.Infrastructure.Attributes;
using Karami.Core.Infrastructure.Implementations;
using Karami.Core.Persistence.Interceptors;
using Karami.Core.UseCase.Contracts.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Nest;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

using ILogger     = Karami.Core.UseCase.Contracts.Interfaces.ILogger;
using Environment = System.Environment;

namespace Karami.Core.Infrastructure.Extensions;

public static class WebApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterHelpers(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(typeof(IDateTime), typeof(DomicDateTime));
        
        builder.Services.AddSingleton(typeof(ISerializer), typeof(Serializer));
        
        builder.Services.AddSingleton(typeof(IJsonWebToken), typeof(JsonWebToken));
        
        builder.Services.AddSingleton(typeof(IGlobalUniqueIdGenerator), typeof(GlobalUniqueIdGenerator));
        
        builder.Services.AddScoped(typeof(ILogger), typeof(Logger));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterELK(this WebApplicationBuilder builder)
    {
        var elasticUri      = Environment.GetEnvironmentVariable("ElasticUri");
        var elasticUsername = Environment.GetEnvironmentVariable("ElasticUsername");
        var elasticPassword = Environment.GetEnvironmentVariable("ElasticPassword");
        
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

        var indexPart_1 = Assembly.GetExecutingAssembly().GetName().Name.ToLower().Replace(".", "-");
        var indexPart_2 = environment?.ToLower().Replace(".", "-");
        
        var elOptions = new ElasticsearchSinkOptions( new Uri(elasticUri) ) {
            AutoRegisterTemplate = true,
            IndexFormat = $"{indexPart_1}-{indexPart_2}-{DateTime.UtcNow:yyyy-MM}"
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
        if(builder.Environment.EnvironmentName.Equals(Karami.Core.Common.ClassConsts.Environment.Testing))
            builder.Services.AddDbContext<TContext>(config => config.UseInMemoryDatabase("Testing-CommandDatabase"));
        else
            builder.Services.AddDbContext<TContext>((provider, config) => 
                config.UseSqlServer(builder.Configuration.GetCommandSqlServerConnectionString())
                      .AddInterceptors(provider.GetRequiredService<EfOutBoxPublishEventInterceptor<TIdentity>>())
            );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TContext"></typeparam>
    public static void RegisterEntityFrameworkCoreQuery<TContext>(this WebApplicationBuilder builder) 
        where TContext : DbContext
    {
        if(builder.Environment.EnvironmentName.Equals(Karami.Core.Common.ClassConsts.Environment.Testing))
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
    /// <returns></returns>
    public static void RegisterRedisCaching(this WebApplicationBuilder builder)
    {
        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
        
        //Third party ( Redis )
        builder.Services.AddScoped<IConnectionMultiplexer>(
            Provider => ConnectionMultiplexer.Connect(
                builder.Configuration.GetRedisConnectionString() 
            )
        );
        
        //Pure
        builder.Services.AddScoped(
            typeof(IRedisCache),
            typeof(RedisCache)
        );
        
        //Pure ( Mediator for cache )
        builder.Services.AddTransient(
            typeof(ICacheService),
            typeof(CacheService)
        );
        
        RegisterAllCachesHandler(builder.Services, useCaseAssemblyTypes);
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
    /// ToDo: must be ( Issuer )   replaced with Host.GetIPAddress()
    /// ToDo: must be ( Audience ) replaced with Host.GetIPAddress()
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
            
            /*در این قسمت موقع ارسال درخواست به سرور ، موارد ( اطلاعات ) موجود در سرور با اطلاعات ارسالی ( توکن ) بررسی می گردد*/
            /*در صورت وجود هر گونه تفاوتی بین داده های تنظیم شده در سرور با اطلاعات ارسالی از سمت کاربر ( توکن ) ؛ اعتبارسنجی کاربر نامعتبر خواهد شد*/
            config.TokenValidationParameters = new TokenValidationParameters {
                ValidIssuer              = builder.Configuration.GetValue<string>("JWT:Issuer"),   /*صادر کننده*/
                ValidAudience            = builder.Configuration.GetValue<string>("JWT:Audience"), /*مصرف کننده*/
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes( builder.Configuration.GetValue<string>("JWT:Key") )),
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime         = true
            };

            /*در این قسمت بررسی می شود که در صورت بروز هر گونه خطایی از سمت سرور ، چه واکنش مناسبی به کلاینت ( کاربر ) ارسال گردد*/
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
                    
                    /*در این قسمت ؛ صحت توکن ارسالی کاربر بررسی می گردد و در صورت نادرست بودن توکن ارسالی ؛ خطای مناسب برای کاربر صادر می گردد*/
                    if (context.Exception.GetType() == typeof(SecurityTokenSignatureKeyNotFoundException)) throw new TokenNotValidException();
                        
                    /*در این قسمت ؛ دلیل عدم موفقیت آمیز بودن احراز هویت ، منقضی شدن زمان توکن می باشد که باید در این صورت خطای مناسب به کاربر صادر گردد*/
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException)) throw new TokenExpireException();

                    return Task.CompletedTask;
                    
                }
                
                ,
                
                /*این قسمت مربوط به سطوح دسترسی یا همان ACL می باشد*/
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
        builder.Services.AddScoped(typeof(IServiceDiscovery), typeof(ServiceDiscovery));

        builder.Services.AddGrpcClient<DiscoveryService.DiscoveryServiceClient>(options => {
                
            options.Address = new Uri(Environment.GetEnvironmentVariable("ServiceDiscoveryAddress"));

        })
        .AddCallCredentials((context, metadata, serviceProvider) => {
            
            metadata.Add(Header.License, builder.Configuration.GetValue<string>("SecretKey"));

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
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Karami.Infrastructure")).GetTypes();
        
        RegisterAllServicesOfGrpcClientWebRequest(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterServicesOfHttpWebRequest(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Karami.Infrastructure")).GetTypes();
        
        RegisterAllServicesOfHttpWebRequest(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterCommandRepositories(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Karami.Infrastructure")).GetTypes();
        
        RegisterAllCommandRepositories(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    public static void RegisterQueryRepositories(this WebApplicationBuilder builder)
    {
        Type[] infrastructureAssemblyTypes = Assembly.Load(new AssemblyName("Karami.Infrastructure")).GetTypes();
        
        RegisterAllQueryRepositories(builder.Services, infrastructureAssemblyTypes);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <typeparam name="TICommandUnitOfWork"></typeparam>
    public static void RegisterCommandQueryUseCases(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient(typeof(IMediator), typeof(Mediator));
        builder.Services.AddSingleton(typeof(IAsyncCommandBroker), typeof(AsyncCommandBroker));
        
        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
        
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
    public static void RegisterMessageBroker(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(typeof(IMessageBroker), typeof(MessageBroker));

        Type[] useCaseAssemblyTypes = Assembly.Load(new AssemblyName("Karami.UseCase")).GetTypes();
        
        RegisterAllConsumerMessageBusHandlers(builder.Services, useCaseAssemblyTypes);
        RegisterAllConsumerEventBusHandler(builder.Services, useCaseAssemblyTypes);
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

    #region AutoRegisterCacheHandler

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="useCaseAssemblyTypes"></param>
    private static void RegisterAllCachesHandler(IServiceCollection serviceCollection, Type[] useCaseAssemblyTypes)
    {
        var cacheHandlerTypes = useCaseAssemblyTypes.Where(
            type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMemoryCacheSetter<>))
        );

        foreach (Type cacheHandlerType in cacheHandlerTypes) {
                
            Type cacheHandlerTypeValue = cacheHandlerType.GetInterfaces().FirstOrDefault().GetGenericArguments().FirstOrDefault();
                
            serviceCollection.AddTransient(
                typeof(IMemoryCacheSetter<>).MakeGenericType(cacheHandlerTypeValue),
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