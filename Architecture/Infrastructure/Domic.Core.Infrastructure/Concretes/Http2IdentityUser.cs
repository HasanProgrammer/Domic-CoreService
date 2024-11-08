using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.Infrastructure.Concretes;

public class Http2IdentityUser(IJsonWebToken jsonWebToken) : IIdentityUser
{
    private string _authToken;

    public void SetAuthToken(string token)
    {
        if (string.IsNullOrEmpty(_authToken))
            _authToken = token;
    }

    public string GetIdentity() => jsonWebToken.GetIdentityUserId(_authToken);

    public string GetUsername() => jsonWebToken.GetUsername(_authToken);

    public List<string> GetRoles() => jsonWebToken.GetRoles(_authToken);
}