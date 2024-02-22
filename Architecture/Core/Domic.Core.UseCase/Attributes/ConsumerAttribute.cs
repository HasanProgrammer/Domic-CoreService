#pragma warning disable CS8618

namespace Domic.Core.UseCase.Commons.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ConsumerAttribute : Attribute
{
    public string Queue { get; set; }
}