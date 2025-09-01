namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithCachingAttribute : Attribute
{
    public int Ttl { get; set; }
    public string Key { get; set; }
}