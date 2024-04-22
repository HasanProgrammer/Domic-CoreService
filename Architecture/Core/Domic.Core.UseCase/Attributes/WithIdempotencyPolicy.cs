using System.Data;

namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithIdempotencyPolicy : Attribute
{
    public IsolationLevel TransactionIsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}