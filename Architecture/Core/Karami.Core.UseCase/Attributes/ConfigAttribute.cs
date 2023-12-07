namespace Karami.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ConfigAttribute : Attribute
{
    /// <summary>
    /// Storage time in minutes .
    /// </summary>
    public int Ttl { get; set; }
    
    /// <summary>
    /// The stored name of the entity inside redis .
    /// </summary>
    public string Key  { get; set; }
}