using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Extensions;

public static class HttpContextExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetToken(this HttpContext Context) 
        => Context.Request.Headers.SingleOrDefault(header => header.Key.Equals("Authorization")).Value;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static string GetRowToken(this HttpContext Context) => Context.GetToken()?.Split("Bearer ")[1];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="Payload"></param>
    public static async Task SendPayloadAsync(this HttpContext Context, object Payload) 
        => await Context.Response.WriteAsync(JsonConvert.SerializeObject(Payload), Encoding.UTF8);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <param name="Type"></param>
    /// <returns></returns>
    public static List<string> GetClaimsToken(this HttpContext Context, string Type)
    {
        string Token = Context.Request.Headers.SingleOrDefault(header => header.Key.Equals("Authorization")).Value;
        
        return (new JwtSecurityTokenHandler().ReadJwtToken(Token.Split("Bearer ")[1])).Claims
            ?.Where(claim => claim.Type.Equals(Type))
            .Select(Claim => Claim.Value)
            .ToList();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="Context"></param>
    /// <returns></returns>
    public static object GetIdentityUserId(this HttpContext Context)
        => Context.GetClaimsToken("UserId").FirstOrDefault();
}