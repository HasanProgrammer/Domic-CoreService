namespace Domic.Core.Common.ClassModels;

public class Throttle
{
    public bool Active { get; set; }
    public string Queue { get; set; }
    public uint Size { get; set; }
    public ushort Limitation { get; set; }
    public bool IsGlobally { get; set; }
}

public class QueueConfig
{
    public List<Throttle> Throttles { get; set; }
}