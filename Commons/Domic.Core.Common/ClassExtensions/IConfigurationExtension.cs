using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Domic.Core.Common.ClassExtensions;

//RabbitMQ
public static partial class IConfigurationExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetExternalRabbitUsername(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("E-RabbitMQ-Username");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetInternalRabbitUsername(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("I-RabbitMQ-Username");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetExternalRabbitPassword(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("E-RabbitMQ-Password");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetInternalRabbitPassword(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("I-RabbitMQ-Password");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetExternalRabbitHostName(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("E-RabbitMQ-Host");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetInternalRabbitHostName(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("I-RabbitMQ-Host");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetExternalRabbitPort(this IConfiguration configuration)
        => Convert.ToInt32(Environment.GetEnvironmentVariable("E-RabbitMQ-Port"));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetInternalRabbitPort(this IConfiguration configuration)
        => Convert.ToInt32(Environment.GetEnvironmentVariable("I-RabbitMQ-Port"));
}

//Database
public static partial class IConfigurationExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetCommandSqlServerConnectionString(this IConfiguration configuration)
        => Environment.GetEnvironmentVariable("C-SqlServerConnectionString");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetQuerySqlServerConnectionString(this IConfiguration configuration) 
        => Environment.GetEnvironmentVariable("Q-SqlServerConnectionString");

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetMongoConnectionString(this IConfiguration configuration) 
        => Environment.GetEnvironmentVariable("MongoConnectionString");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hostEnvironment"></param>
    /// <returns></returns>
    public static string GetRedisConnectionString(this IConfiguration configuration) 
        => Environment.GetEnvironmentVariable("RedisConnectionString");
}

//Webservice
public static partial class IConfigurationExtension
{
    public static string GetNotificationServiceHubUrl(this IConfiguration configuration, 
        IHostEnvironment hostEnvironment
    ) => configuration.GetValue<string>(
        $"TCP:{hostEnvironment.EnvironmentName}.NotificationService"
    );
}

//JsonResult
public static partial class IConfigurationExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetSuccessStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:Success");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetSuccessCreateStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:SuccessCreate");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetErrorStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:Error");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetServerErrorStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:ServerError");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetUnAuthorizedStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:UnAuthorized");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static int GetForbiddenStatusCode(this IConfiguration configuration) 
        => configuration.GetValue<int>("StatusCode:Forbidden");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetSuccessFetchDataMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:SuccessFetchData");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetSuccessCreateMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:SuccessCreate");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetSuccessUpdateMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:SuccessUpdate");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetSuccessDeleteMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:SuccessDelete");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetErrorCreateMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ErrorCreate");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetErrorUpdateMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ErrorUpdate");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetErrorDeleteMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ErrorDelete");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetModelValidationMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ModelValidation");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetNotFoundMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:NotFound");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetRequiredFieldMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:RequiredField");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetNotUniqueFieldMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:NotUniqueField");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetSuccessSignInMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:SuccessSignIn");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetErrorSignInMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ErrorSignIn");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetServerErrorMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:ServerError");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetTokenExpireMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:TokenExpire");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetTokenNotValidMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:TokenNotValid");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetChallengeMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:Challenge");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetUnAuthorizedMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:UnAuthorized");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetForbiddenMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:Forbidden");
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static string GetInValidMediaTypeMessage(this IConfiguration configuration) 
        => configuration.GetValue<string>("Message:FA:InValidMediaType");
}