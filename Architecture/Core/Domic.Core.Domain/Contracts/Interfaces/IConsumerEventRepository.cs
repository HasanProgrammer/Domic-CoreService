using Domic.Core.Domain.Entities;

namespace Domic.Core.Domain.Contracts.Interfaces;

//this repository is for all sides ( query & command )
public interface IConsumerEventRepository : IRepository<ConsumerEvent>;