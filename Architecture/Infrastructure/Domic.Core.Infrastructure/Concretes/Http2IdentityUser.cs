using Domic.Core.Domain.Contracts.Interfaces;
using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.Infrastructure.Concretes;

public sealed class Http2IdentityUser(IJsonWebToken jsonWebToken) : IIdentityUser
{
    private string _authToken;

    public void SetAuthToken(string token)
    {
        if (string.IsNullOrEmpty(_authToken))
            _authToken = token;
    }

    public string GetIdentity() => !string.IsNullOrEmpty(_authToken) ? jsonWebToken.GetUserIdentity(_authToken) : "System";

    public string GetUsername() => !string.IsNullOrEmpty(_authToken) ? jsonWebToken.GetUsername(_authToken) : "System";

    public List<string> GetRoles() => !string.IsNullOrEmpty(_authToken) ? jsonWebToken.GetRoles(_authToken) : [];
}