using Domic.Core.UseCase.Contracts.Interfaces;

namespace Domic.Core.UseCase.Contracts.Abstracts;

public abstract class CreateAsyncCommand : IAsyncCommand
{
    public string CommandId { get; set; }
    public string ConnectionId { get; set; }
}