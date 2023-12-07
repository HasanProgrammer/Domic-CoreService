using Karami.Core.UseCase.DTOs;

namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IJsonWebToken
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="tokenParameter"></param>
    /// <param name="claims"></param>
    /// <returns></returns>
    public string Generate(TokenParameterDto tokenParameter, params KeyValuePair<string, string>[] claims);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public string GetIdentityUserId(string token);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public string GetUsername(string token);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public List<string> GetRoles(string token);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public List<string> GetPermissions(string token);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public List<string> GetClaimsToken(string token, string claimType);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rowToken"></param>
    /// <param name="claimType"></param>
    /// <returns></returns>
    public List<string> GetClaimsRowToken(string rowToken, string claimType);
}