#pragma warning disable CS8618

namespace Domic.Core.UseCase.Commons.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StreamConsumerAttribute : Attribute
{
    public string Topic { get; set; }
}