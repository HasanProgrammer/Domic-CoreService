using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Karami.Core.Infrastructure.Extensions;

public static class IApplicationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="webHostEnvironment"></param>
    public static void UseCustomSwagger(this IApplicationBuilder builder, IWebHostEnvironment webHostEnvironment)
    {
        if (webHostEnvironment.IsDevelopment())
        {
            builder.UseSwagger(options => {
                options.SerializeAsV2 = true;
            });
            
            builder.UseSwaggerUI(options => {
                options.ConfigObject.Filter                   = "";
                options.ConfigObject.PersistAuthorization     = true;
                options.ConfigObject.DisplayRequestDuration   = true;
                options.ConfigObject.DefaultModelsExpandDepth = -1;
                options.ConfigObject.DocExpansion             = DocExpansion.None;
                options.ConfigObject.DefaultModelRendering    = ModelRendering.Example;

                options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            });
        }
    }
}