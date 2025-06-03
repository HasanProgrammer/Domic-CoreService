namespace Domic.Core.Domain.Entities;

//Serializable
public class EventStore<TIdentity>
{
    public TIdentity Id          { get; set; }
    public TIdentity AggregateId { get; set; }
    public string NameOfService  { get; set; } //Name Of Service
    public string NameOfEvent    { get; set; } //Name Of Event
    public string Action         { get; set; } //CREATE | UPDATE | DELETE
    public string Payload        { get; set; }
    public TIdentity CreatedBy   { get; set; }
    public string CreatedRole    { get; set; }
    public DateTime CreatedAt    { get; set; }
}