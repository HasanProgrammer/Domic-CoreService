using Karami.Core.Domain.Contracts.Interfaces;
using MD.PersianDateTime.Standard;

namespace Karami.Core.Domain.Implementations;

public class DotrisDateTime : IDotrisDateTime
{
    public string ToPersianShortDate(DateTime dateTime) => new PersianDateTime(dateTime).ToShortDateString();
}