namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IAsyncValidator<in TInput>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object Validate(TInput input) => throw new NotImplementedException();
}