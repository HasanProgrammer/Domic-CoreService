using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Abstracts;

public abstract class UpdateAsyncCommand : IAsyncCommand
{
    public string ConnectionId { get; set; }
}