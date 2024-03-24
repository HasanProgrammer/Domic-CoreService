using System.Collections.ObjectModel;

namespace Domic.Core.UseCase.Contracts.Abstracts;

public abstract class Auditable
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public required ReadOnlyCollection<string> UserRoles { get; init; }
    public required ReadOnlyCollection<string> UserPermissions { get; init; }
}