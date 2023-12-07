namespace Karami.Core.Domain.Contracts.Abstracts;

public abstract class ValueObject
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    private static bool BaseEqual(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;

        return ReferenceEquals(left, right) || left.Equals(right);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    private static bool BaseNotEqual(ValueObject left, ValueObject right) => !BaseEqual(left, right);
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    /*---------------------------------------------------------------*/

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        ValueObject right = (ValueObject) obj;

        return GetEqualityComponents().SequenceEqual(right.GetEqualityComponents());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
        => GetEqualityComponents().Select(c => c != null ? c.GetHashCode() : 0)
                                  .Aggregate((x, y) => x ^ y);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(ValueObject left, ValueObject right) => BaseEqual(left, right);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(ValueObject left, ValueObject right) => BaseNotEqual(left, right);
}