#pragma warning disable SYSLIB0021

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domic.Core.Domain.Extensions;

public static class StringExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="phone"></param>
    /// <returns></returns>
    public static bool IsValidMobileNumber(this string phone)
    {
        const string pattern = @"^09[0|1|2|3][0-9]{8}$";
        return new Regex(pattern).IsMatch(phone);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public static bool IsValidEmail(this string email)
    {
        const string pattern = @"[\w-\.]+@([\w-]+\.)+[\w-]{2,4}";
        return new Regex(pattern).IsMatch(email);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string Search(this string source, string target)
    {
        int index = source.IndexOf(target);

        return index != -1 ? source.Substring(index + target.Length) : "";
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Input"></param>
    /// <returns></returns>
    public static async Task<string> HashAsync(this string Input)
    {
        string filterInput = new string(Input.Where(c => !char.IsControl(c)).ToArray());
        
        await using MemoryStream Stream = new(Encoding.ASCII.GetBytes(filterInput));
        
        #pragma warning disable CS0618
        byte[] Result = await new SHA512CryptoServiceProvider().ComputeHashAsync(Stream);
        #pragma warning restore CS0618

        StringBuilder Builder = new();
        foreach (byte item in Result)
            Builder.Append(item.ToString("X2"));
        return Builder.ToString();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<string> HashAsync(this string Input, CancellationToken cancellationToken)
    {
        string filterInput = new string(Input.Where(c => !char.IsControl(c)).ToArray());
        
        await using MemoryStream Stream = new(Encoding.ASCII.GetBytes(filterInput));
        
        #pragma warning disable CS0618
        byte[] Result = await new SHA512CryptoServiceProvider().ComputeHashAsync(Stream, cancellationToken);
        #pragma warning restore CS0618

        StringBuilder Builder = new();
        foreach (byte item in Result)
            Builder.Append(item.ToString("X2"));
        return Builder.ToString();
    }
}