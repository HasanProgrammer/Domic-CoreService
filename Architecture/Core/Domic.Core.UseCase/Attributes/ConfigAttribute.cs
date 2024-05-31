namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ConfigAttribute : Attribute
{
    /// <summary>
    /// storage time in minutes .
    /// </summary>
    public int Ttl { get; set; }
    
    /// <summary>
    /// the stored name of the entity inside redis .
    /// </summary>
    public string Key { get; set; }
}