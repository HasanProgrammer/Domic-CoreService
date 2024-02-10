namespace Domic.Core.Common.ClassModels;

public class ServiceStatus
{
    public string Name      { get; set; }
    public string Host      { get; set; }
    public string IPAddress { get; set; }
    public string Port      { get; set; }
    public bool Status      { get; set; }
    public int ResponseTime { get; set; }
}