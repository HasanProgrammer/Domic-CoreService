using Karami.Core.UseCase.Contracts.Interfaces;

namespace Karami.Core.UseCase.Contracts.Abstracts;

public abstract class CreateAsyncCommand : IAsyncCommand
{
    public string ConnectionId { get; set; }
}