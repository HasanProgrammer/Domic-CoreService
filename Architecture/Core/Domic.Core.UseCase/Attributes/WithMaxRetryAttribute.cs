namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithMaxRetryAttribute : Attribute
{
    public int Count { get; set; } = 200;
    public bool HasAfterMaxRetryHandle { get; set; } = false;
}