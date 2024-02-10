#pragma warning disable SYSLIB0021

using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Extensions;

public static class StringExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T DeSerialize<T>(this string Input)
        => JsonConvert.DeserializeObject<T>(Input, new JsonSerializerSettings {
            Formatting = Formatting.Indented
        });
}