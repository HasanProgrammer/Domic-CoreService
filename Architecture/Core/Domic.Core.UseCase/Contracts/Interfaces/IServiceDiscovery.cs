using Domic.Core.Common.ClassModels;

namespace Domic.Core.UseCase.Contracts.Interfaces;

public interface IServiceDiscovery : IDisposable
{
    /// <summary>
    /// This function outputs information about all the services in the system .
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<List<ServiceStatus>> FetchAllServicesInfoAsync(CancellationToken cancellationToken) 
        => throw new NotImplementedException();
    
    /// <summary>
    /// This function uses a specific [ LoadBalance ] algorithm between all urls of a specific service and finally
    /// outputs a specific url based on that . This operation uses the data in the [ RedisCache ] .
    /// Hint : performing [ LoadBalance ] inside this method
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<string> LoadAddressInMemoryAsync(string serviceName, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    /// <summary>
    /// This function uses a specific [ LoadBalance ] algorithm between all urls of a specific service and finally
    /// outputs a specific url based on that .
    /// Hint : performing [ LoadBalance ] in [ DiscoveryService ]
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<string> LoadAddressAsync(string serviceName, CancellationToken cancellationToken)
        => throw new NotImplementedException();
    
    /// <summary>
    /// This function outputs all available urls for a specific service, regardless of the specified algorithm
    /// for [ LoadBalance ] between them .
    /// </summary>
    /// <param name="serviceName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<List<string>> LoadAddressesAsync(string serviceName, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}