namespace Karami.Core.Domain.Contracts.Interfaces;

public interface IGlobalUniqueIdGenerator
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetRandom() => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string GetRandom(int count) => throw new NotImplementedException();
}