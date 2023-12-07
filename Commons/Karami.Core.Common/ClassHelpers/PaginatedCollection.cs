namespace Karami.Core.Common.ClassHelpers;

public class PaginatedCollection<T>
{
    public int PageNumber { get; set; } /*شماره صفحه فعلی*/
    
    public int CountItemPerPage { get; set; } /*تعداد داده های قابل نمایش برای هر صفحه*/
    
    public int TotalPages { get; set; } /*تعداد کل صفحات بر اساس تعداد ردیف های Entity مورد نظر و با در نظر گرفتن CountSizePerPage*/

    /*----------------------------------------------------------*/
        
    public bool HasPrev => PageNumber > 1;          /*در این قسمت بررسی می شود که آیا لینک صفحه قبلی فعال باشد یا خیر*/
    public bool HasNext => PageNumber < TotalPages; /*در این قسمت بررسی می شود که آیا لینک صفحه بعدی فعال باشد یا خیر*/

    /*----------------------------------------------------------*/
    
    public IEnumerable<T> Collection { get; set; }

    /*----------------------------------------------------------*/

    public PaginatedCollection() {}
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="countRowData"></param>
    /// <param name="countItemPerPage"></param>
    /// <param name="pageNumber"></param>
    /// <param name="paginating"></param>
    public PaginatedCollection(IEnumerable<T> data, long countRowData, int countItemPerPage, int pageNumber, 
        bool paginating = true
    )
    {
        PageNumber       = pageNumber;
        CountItemPerPage = countItemPerPage;
        TotalPages       = (int)Math.Ceiling(countRowData / (double) countItemPerPage);
        Collection       = paginating ? data.Skip((pageNumber - 1)*countItemPerPage).Take(countItemPerPage) : data;
    }
}