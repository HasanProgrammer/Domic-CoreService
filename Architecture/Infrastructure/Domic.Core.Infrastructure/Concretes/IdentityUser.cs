using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.Infrastructure.Extensions;
using Domic.Core.UseCase.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Domic.Core.Infrastructure.Concretes;

public class IdentityUser : IIdentityUser
{
    private readonly IJsonWebToken _jsonWebToken;
    private readonly string _authToken;
    
    public IdentityUser(IJsonWebToken jsonWebToken, IHttpContextAccessor httpContextAccessor)
    {
        _jsonWebToken = jsonWebToken;
        _authToken = httpContextAccessor.HttpContext.GetRowToken();
    }

    public string GetIdentity() => _jsonWebToken.GetIdentityUserId(_authToken);

    public string GetUsername() => _jsonWebToken.GetUsername(_authToken);

    public List<string> GetRoles() => _jsonWebToken.GetRoles(_authToken);
}