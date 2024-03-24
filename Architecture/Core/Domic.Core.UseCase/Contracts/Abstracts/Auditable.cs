using System.Collections.ObjectModel;

namespace Domic.Core.UseCase.Contracts.Abstracts;

public abstract class Auditable
{
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public required ReadOnlyCollection<string> UserRoles { get; set; }
    public required ReadOnlyCollection<string> UserPermissions { get; set; }
}