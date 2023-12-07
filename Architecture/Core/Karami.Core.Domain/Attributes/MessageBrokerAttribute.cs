using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class MessageBrokerAttribute : Attribute
{
    public string Queue          { get; set; }
    public string Route          { get; set; }
    public string Exchange       { get; set; }
    public Exchange ExchangeType { get; set; }
}