namespace Domic.Core.Common.ClassModels;

public class TopicThrottle
{
    public bool Active { get; set; }
    public string Topic { get; set; }
    public ushort Limitation { get; set; }
}

public class TopicConfig
{
    public List<TopicThrottle> Throttle { get; set; }
}