namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithDistributedPessimisticLockAttribute : Attribute
{
    public string Key { get; set; }
}