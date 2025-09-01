namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IAsyncValidator<in TInput>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public object Validate(TInput input) => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<object> ValidateAsync(TInput input, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}