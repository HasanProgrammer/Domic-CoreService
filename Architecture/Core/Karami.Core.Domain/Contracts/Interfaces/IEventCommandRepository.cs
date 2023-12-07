using Karami.Core.Domain.Entities;

namespace Karami.Core.Domain.Contracts.Interfaces;

public interface IEventCommandRepository : ICommandRepository<Event, string>
{
    
}