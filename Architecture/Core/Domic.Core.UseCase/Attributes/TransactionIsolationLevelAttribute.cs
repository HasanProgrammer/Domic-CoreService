using System.Data;

namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class TransactionIsolationLevelAttribute : Attribute
{
    public IsolationLevel Level { get; set; } = IsolationLevel.ReadCommitted;
}