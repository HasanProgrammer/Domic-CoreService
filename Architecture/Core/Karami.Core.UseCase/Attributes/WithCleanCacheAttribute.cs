namespace Karami.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithCleanCacheAttribute : Attribute
{
    public string Keies { get; set; } //Pattern : Key1|Key2|Key3...
}