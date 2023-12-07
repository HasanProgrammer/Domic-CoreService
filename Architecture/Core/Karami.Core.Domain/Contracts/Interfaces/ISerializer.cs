namespace Karami.Core.Domain.Contracts.Interfaces;

public interface ISerializer
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="object"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string Serialize<T>(T @object) => throw new NotImplementedException();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public T DeSerialize<T>(string source) => throw new NotImplementedException();
}