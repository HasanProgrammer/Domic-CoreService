namespace Domic.Core.Domain.Entities;

public class ConsumerEvent
{
    public string Id { get; set; }
    public string Type { get; set; }
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate { get; set; }
}