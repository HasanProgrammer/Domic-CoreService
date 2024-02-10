using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.Common.ClassExtensions;

public static class IConfigurationBuilderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ConfigurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddCoreEnvConfig(this IConfigurationBuilder ConfigurationBuilder, 
        IHostEnvironment hostEnvironment
    )
    {
        string destPath = Path.Combine(hostEnvironment.ContentRootPath, ".env");
        string srcPath  = Path.GetFullPath(@$"..\..\..\..\..\Services\CoreService\Commons\Karami.Core.Config\EnvConfigs\.env");

        if (File.Exists(srcPath))
        {
            if(File.Exists(destPath)) File.Delete(destPath);
                
            File.Copy(srcPath, destPath);
        }
        
        return ConfigurationBuilder;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ConfigurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddRabbitConfig(this IConfigurationBuilder ConfigurationBuilder, 
        IHostEnvironment hostEnvironment
    )
    {
        string destPath = Path.Combine(hostEnvironment.ContentRootPath, "Configs", "MessageBroker.json");
        string srcPath  = Path.GetFullPath(@"..\..\..\..\..\Services\CoreService\Commons\Karami.Core.Config\JsonConfigs\MessageBroker.json");

        if (File.Exists(srcPath))
        {
            if(File.Exists(destPath)) File.Delete(destPath);
                
            File.Copy(srcPath, destPath);
        }

        ConfigurationBuilder.AddJsonFile(destPath, optional: true, reloadOnChange: true);
        
        return ConfigurationBuilder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ConfigurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddJsonResultConfig(this IConfigurationBuilder ConfigurationBuilder, 
        IHostEnvironment hostEnvironment
    )
    {
        string destPath = Path.Combine(hostEnvironment.ContentRootPath, "Configs", "JsonResult.json");
        string srcPath  = Path.GetFullPath(@"..\..\..\..\..\Services\CoreService\Commons\Karami.Core.Config\JsonConfigs\JsonResult.json");
        
        if (File.Exists(srcPath))
        {
            if(File.Exists(destPath)) File.Delete(destPath);
                
            File.Copy(srcPath, destPath);
        }
        
        ConfigurationBuilder.AddJsonFile(destPath, optional: true, reloadOnChange: true);
        
        return ConfigurationBuilder;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ConfigurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddLicenseConfig(this IConfigurationBuilder ConfigurationBuilder, 
        IHostEnvironment hostEnvironment
    )
    {
        string destPath = Path.Combine(hostEnvironment.ContentRootPath, "Configs", "License.json");
        string srcPath  = Path.GetFullPath(@"..\..\..\..\..\Services\CoreService\Commons\Karami.Core.Config\JsonConfigs\License.json");
        
        if (File.Exists(srcPath))
        {
            if(File.Exists(destPath)) File.Delete(destPath);
                
            File.Copy(srcPath, destPath);
        }
        
        ConfigurationBuilder.AddJsonFile(destPath, optional: true, reloadOnChange: true);
        
        return ConfigurationBuilder;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ConfigurationBuilder"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static IConfigurationBuilder AddWebServiceConfig(this IConfigurationBuilder ConfigurationBuilder, 
        IHostEnvironment hostEnvironment
    )
    {
        string destPath = Path.Combine(hostEnvironment.ContentRootPath, "Configs", "WebService.json");
        string srcPath  = Path.GetFullPath(@"..\..\..\..\..\Services\CoreService\Commons\Karami.Core.Config\JsonConfigs\WebService.json");
        
        if (File.Exists(srcPath))
        {
            if(File.Exists(destPath)) File.Delete(destPath);
                
            File.Copy(srcPath, destPath);
        }
        
        ConfigurationBuilder.AddJsonFile(destPath, optional: true, reloadOnChange: true);
        
        return ConfigurationBuilder;
    }
}