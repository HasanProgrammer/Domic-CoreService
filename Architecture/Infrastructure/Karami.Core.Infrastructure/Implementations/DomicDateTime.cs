using Karami.Core.Domain.Contracts.Interfaces;
using MD.PersianDateTime.Standard;

namespace Karami.Core.Infrastructure.Implementations;

public class DomicDateTime : IDateTime
{
    public string ToPersianShortDate(DateTime dateTime) => new PersianDateTime(dateTime).ToShortDateString();
}