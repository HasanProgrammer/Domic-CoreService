using Domic.Core.Domain.Contracts.Abstracts;

namespace Domic.Core.Domain.Entities;

public class ConsumerEvent : BaseEntity<string>
{
    public new string Id { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate { get; set; }
}