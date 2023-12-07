using Karami.Core.Common.ClassHelpers;

namespace Karami.Core.Common.ClassExtensions;

public static class ListExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="list"></param>
    /// <param name="total"></param>
    /// <param name="countSizePerPage"></param>
    /// <param name="currentPageNumber"></param>
    /// <param name="paginating"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static PaginatedCollection<T> ToPaginatedCollection<T>(this IEnumerable<T> list, long total , 
        int countSizePerPage, int currentPageNumber, bool paginating = true
    ) => new(list, total, countSizePerPage, currentPageNumber, paginating);
}