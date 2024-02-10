using Domic.Core.Domain.Enumerations;

namespace Domic.Core.UseCase.DTOs;

public class MessageBrokerDto<TMessage> where TMessage : class
{
    public TMessage Message      { get; set; }
    public string Queue          { get; set; }
    public string Route          { get; set; }
    public string Exchange       { get; set; }
    public Exchange ExchangeType { get; set; }
    
    public Dictionary<string, object> Headers { get; set; }
}