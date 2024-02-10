using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Extensions;

public static class ObjectExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static string Serialize<T>(this T Input) => JsonConvert.SerializeObject(Input, Formatting.Indented);
}