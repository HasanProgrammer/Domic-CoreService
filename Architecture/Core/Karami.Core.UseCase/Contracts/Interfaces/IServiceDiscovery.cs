namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface IServiceDiscovery : IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<string> LoadAddressAsync(string serviceName, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<List<string>> LoadAddressesAsync(string serviceName, CancellationToken cancellationToken) 
        => throw new NotImplementedException();
}