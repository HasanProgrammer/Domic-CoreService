using Karami.Core.UseCase.Contracts.Interfaces;

namespace Karami.Core.UseCase.Contracts.Abstracts;

public abstract class DeleteAsyncCommand : IAsyncCommand
{
    public string ConnectionId { get; set; }
}