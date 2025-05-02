using System.Data;
using Domic.Core.Common.ClassEnums;

namespace Domic.Core.UseCase.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class WithTransactionAttribute : Attribute
{
    public TransactionType Type { get; set; } = TransactionType.Command;
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}