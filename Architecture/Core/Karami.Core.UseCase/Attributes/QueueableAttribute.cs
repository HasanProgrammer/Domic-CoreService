namespace Karami.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class QueueableAttribute : Attribute
{
    public string Queue    { get; set; }
    public string Exchange { get; set; }
    public string Route    { get; set; }
}