using Domic.Core.Domain.Entities;

namespace Domic.Core.Domain.Contracts.Interfaces;

public interface IQueryConsumerEventRepository : IQueryRepository<ConsumerEventQuery, string>;