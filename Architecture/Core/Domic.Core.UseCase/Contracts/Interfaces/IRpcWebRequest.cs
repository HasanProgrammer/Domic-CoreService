namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IRpcWebRequest : IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<bool> CheckExistAsync(string id, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}