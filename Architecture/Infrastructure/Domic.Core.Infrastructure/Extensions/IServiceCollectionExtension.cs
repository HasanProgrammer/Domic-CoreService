using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Domic.Core.Infrastructure.Extensions;

public static class IServiceCollectionExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options => {
            
            options.CustomSchemaIds(type => type.ToString());

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
                Name         = "Authorization"           ,
                Scheme       = "Bearer"                  ,
                BearerFormat = "JWT"                     ,
                In           = ParameterLocation.Header  ,
                Type         = SecuritySchemeType.ApiKey ,
                Description  = ""
            });
            
            options.AddSecurityRequirement(new OpenApiSecurityRequirement {
                {
                    new OpenApiSecurityScheme {
                        Reference = new OpenApiReference {
                            Id   = "Bearer",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    new List<string>()
                }
            });
            
            options.SwaggerDoc("v1", new OpenApiInfo {
                Version     = "v1"             ,
                Title       = "Domic Gateway" ,
                Description = "Domic Web-API-Gateway"
            });
            
        });

        return services;
    }
}