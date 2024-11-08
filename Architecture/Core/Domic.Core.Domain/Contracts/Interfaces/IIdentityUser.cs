namespace Domic.Core.Domain.Contracts.Interfaces;

public interface IIdentityUser
{
    public string GetIdentity();
    public string GetUsername();
    public List<string> GetRoles();
}