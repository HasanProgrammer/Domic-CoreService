using System.Data;

namespace Karami.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithTransactionAttribute : Attribute
{
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}