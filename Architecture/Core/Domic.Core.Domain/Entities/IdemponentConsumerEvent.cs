using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Enumerations;

namespace Domic.Core.Domain.Entities;

public class IdemponentConsumerEvent : BaseEntityQuery<string>
{
    public new string Id { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }
    public IsActive IsActive { get; set; } = IsActive.Active;
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate { get; set; }
    public DateTime? UpdatedAt_EnglishDate { get; set; }
    public string UpdatedAt_PersianDate { get; set; }
}