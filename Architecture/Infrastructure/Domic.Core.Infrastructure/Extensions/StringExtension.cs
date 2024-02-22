#pragma warning disable SYSLIB0021

using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Extensions;

public static class StringExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T DeSerialize<T>(this string input)
        => JsonConvert.DeserializeObject<T>(input, new JsonSerializerSettings {
            Formatting = Formatting.Indented
        });
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    public static object DeSerialize(this string input, Type destination)
        => JsonConvert.DeserializeObject(input, destination, new JsonSerializerSettings {
            Formatting = Formatting.Indented
        });
}