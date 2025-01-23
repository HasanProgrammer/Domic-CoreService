using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domic.Core.UseCase.Contracts.Interfaces;
using Domic.Core.UseCase.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace Domic.Core.Infrastructure.Concretes;

public class JsonWebToken : IJsonWebToken
{
    public string Generate(TokenParameterDto tokenParameter, params KeyValuePair<string, string>[] claims)
    {
        var tokenDescriptor = GetTokenDescriptor(tokenParameter, claims);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);

        string token = tokenHandler.WriteToken(securityToken);

        return token;
    }

    public string GetUserIdentity(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token ?? "")
                                        .Claims?
                                        .Where(claim => claim.Type.Equals("user_identity")).Select(Claim => Claim.Value)
                                        .FirstOrDefault();

    public string GetUsername(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token ?? "")
                                        .Claims?
                                        .Where(claim => claim.Type.Equals("username")).Select(Claim => Claim.Value)
                                        .FirstOrDefault();

    public List<string> GetRoles(string token)
        => new JwtSecurityTokenHandler().ReadJwtToken(token ?? "")
                                        .Claims?
                                        .Where(claim => claim.Type.Equals("role")).Select(Claim => Claim.Value)
                                        .ToList();
    
    public List<string> GetClaimsToken(string token, string claimType) 
        => new JwtSecurityTokenHandler().ReadJwtToken(token?.Split("Bearer ")[1] ?? "")
                                        .Claims?
                                        .Where(claim => claim.Type.Equals(claimType))
                                        .Select(Claim => Claim.Value).ToList();

    public List<string> GetClaimsRowToken(string rowToken, string claimType) 
        => new JwtSecurityTokenHandler().ReadJwtToken(rowToken ?? "")
                                        .Claims?
                                        .Where(claim => claim.Type.Equals(claimType))
                                        .Select(Claim => Claim.Value).ToList();
    
    /*---------------------------------------------------------------*/

    private SecurityTokenDescriptor GetTokenDescriptor(TokenParameterDto tokenParameter, 
        params KeyValuePair<string, string>[] claims
    )
    {
        List<Claim> claimsObject = claims.Select(claim => new Claim(claim.Key, claim.Value)).ToList();

        var tokenDescriptor = new SecurityTokenDescriptor {
            Issuer             = tokenParameter.Issuer            ,
            Audience           = tokenParameter.Audience          , 
            Subject            = new ClaimsIdentity(claimsObject) ,
            Expires            = DateTime.UtcNow.AddMinutes(tokenParameter.Expires),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenParameter.Key)), 
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        return tokenDescriptor;
    }
}