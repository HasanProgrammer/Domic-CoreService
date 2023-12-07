using Karami.Core.Domain.Contracts.Abstracts;
using Karami.Core.Domain.Exceptions;

namespace Karami.Core.Domain.ValueObjects;

public class CreatedAt : ValueObject
{
    public readonly DateTime? EnglishDate;
    public readonly string PersianDate;

    public CreatedAt() {}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="englishDate"></param>
    /// <param name="persianDate"></param>
    /// <exception cref="InValidValueObjectException"></exception>
    public CreatedAt(DateTime? englishDate , string persianDate)
    {
        if (englishDate == null || string.IsNullOrWhiteSpace(persianDate))
            throw new DomainException("فیلد تاریخ ساخت الزامی می باشد !");
        
        EnglishDate = englishDate;
        PersianDate = persianDate;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EnglishDate;
        yield return PersianDate;
    }
}