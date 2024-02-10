using Domic.Core.Domain.Contracts.Abstracts;
using Domic.Core.Domain.Enumerations;

namespace Domic.Core.Domain.Entities;

public class SystemRequest : BaseEntity<string>
{
    public new string Id                   { get; set; }
    public string IpClient                 { get; set; }
    public string Service                  { get; set; }
    public string Action                   { get; set; }
    public string Header                   { get; set; }
    public string Payload                  { get; set; }
    public DateTime CreatedAt_EnglishDate  { get; set; }
    public string CreatedAt_PersianDate    { get; set; }
    public DateTime? UpdatedAt_EnglishDate { get; set; }
    public string UpdatedAt_PersianDate    { get; set; }
    public new IsDeleted IsDeleted         { get; set; } = IsDeleted.UnDelete;
}