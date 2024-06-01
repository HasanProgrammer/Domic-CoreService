using Domic.Core.Domain.Contracts.Abstracts;

namespace Domic.Core.Domain.Entities;

public class ConsumerEventQuery : BaseEntityQuery<string>
{
    public new string Id { get; set; }
    public string Type { get; set; }
    public int CountOfRetry { get; set; }
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate { get; set; }
}