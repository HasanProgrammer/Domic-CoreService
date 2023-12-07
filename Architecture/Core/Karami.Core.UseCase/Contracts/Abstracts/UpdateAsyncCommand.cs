using Karami.Core.UseCase.Contracts.Interfaces;

namespace Karami.Core.UseCase.Contracts.Abstracts;

public abstract class UpdateAsyncCommand : IAsyncCommand
{
    public string ConnectionId { get; set; }
}