#pragma warning disable CS0649

using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Enumerations;

namespace Domic.Core.Domain.Entities;

//Serializable
public class Event : BaseEntity<string>
{
    public new string Id                   { get; set; }
    public string AggregateId              { get; set; }
    public string Type                     { get; set; } //Name Of Event
    public string Service                  { get; set; } //Name Of Service
    public string Payload                  { get; set; }
    public string Table                    { get; set; }
    public string Action                   { get; set; } //CREATE | UPDATE | DELETE
    public DateTime CreatedAt_EnglishDate  { get; set; }
    public string CreatedAt_PersianDate    { get; set; }
    public DateTime? UpdatedAt_EnglishDate { get; set; }
    public string UpdatedAt_PersianDate    { get; set; }
    public IsActive IsActive               { get; set; } = IsActive.Active;
    public new IsDeleted IsDeleted         { get; set; } = IsDeleted.UnDelete;
}