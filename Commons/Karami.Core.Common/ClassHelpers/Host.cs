using System.Net;
using System.Net.Sockets;

namespace Karami.Core.Common.ClassHelpers;

public class Host
{
    public static string GetName() => Dns.GetHostName();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static string GetIPAddress() =>
        Dns.GetHostEntry( GetName() )
           .AddressList
           .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
           .ToString();
}