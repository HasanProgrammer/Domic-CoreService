using Domic.Core.Domain.Contracts.Interfaces;
using MD.PersianDateTime.Standard;

namespace Domic.Core.Infrastructure.Concretes;

public class DomicDateTime : IDateTime
{
    public string ToPersianShortDate(DateTime dateTime) => new PersianDateTime(dateTime).ToShortDateString();
}