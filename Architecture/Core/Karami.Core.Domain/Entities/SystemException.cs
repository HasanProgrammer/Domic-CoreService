using Karami.Core.Domain.Contracts.Abstracts;
using Karami.Core.Domain.Enumerations;

namespace Karami.Core.Domain.Entities;

//Serializable
public class SystemException : BaseEntity<string>
{
    public new string Id                  { get; set; }
    public string Service                 { get; set; }
    public string Action                  { get; set; }
    public string Message                 { get; set; }
    public string Exception               { get; set; }
    public DateTime CreatedAt_EnglishDate { get; set; }
    public string CreatedAt_PersianDate   { get; set; }
    public DateTime UpdatedAt_EnglishDate { get; set; }
    public string UpdatedAt_PersianDate   { get; set; }
    public IsActive IsActive              { get; set; } = IsActive.Active;
    public new IsDeleted IsDeleted        { get; set; } = IsDeleted.UnDelete;
}