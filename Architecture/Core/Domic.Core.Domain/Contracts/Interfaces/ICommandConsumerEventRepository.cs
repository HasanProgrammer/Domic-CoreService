using Domic.Core.Domain.Entities;

namespace Domic.Core.Domain.Contracts.Interfaces;

public interface ICommandConsumerEventRepository : ICommandRepository<ConsumerEvent, string>;