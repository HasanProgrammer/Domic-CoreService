using System.Data;
using Domic.Core.Common.ClassConsts;

namespace Domic.Core.UseCase.Attributes;

//this attribute used in consumers of message brokers
[AttributeUsage(AttributeTargets.Method)]
public class TransactionConfigAttribute : Attribute
{
    public TransactionType Type { get; set; }
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;
}