namespace Domic.Core.UseCase.Contracts.Abstracts;

public abstract class PaginatedQuery
{
    public int? PageNumber { get; set; } = 1;
    public int? CountPerPage { get; set; } = 20;
}