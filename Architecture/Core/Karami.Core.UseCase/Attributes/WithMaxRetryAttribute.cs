namespace Karami.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithMaxRetryAttribute : Attribute
{
    public int Count { get; set; } = 30;
    public bool HasAfterMaxRetryHandle { get; set; } = false;
}