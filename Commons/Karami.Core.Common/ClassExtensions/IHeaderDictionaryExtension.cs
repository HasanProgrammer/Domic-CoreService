using Karami.Core.Common.ClassConsts;
using Microsoft.AspNetCore.Http;

namespace Karami.Core.Common.ClassExtensions;

public static class IHeaderDictionaryExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="header"></param>
    /// <returns></returns>
    public static string Licence(this IHeaderDictionary header) => header[Header.License];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="header"></param>
    /// <returns></returns>
    public static string Token(this IHeaderDictionary header) => header[Header.Token];
}