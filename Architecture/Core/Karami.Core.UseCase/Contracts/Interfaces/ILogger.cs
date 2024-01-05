namespace Karami.Core.UseCase.Contracts.Interfaces;

public interface ILogger
{
    /// <summary>
    /// This service places a unique log service in [ StateTracker ] and in the [ Mongo ] database and the [ Log ]
    /// document
    /// </summary>
    /// <param name="uniqueKey"></param>
    /// <param name="serviceName"></param>
    /// <param name="item"></param>
    public void Record(string uniqueKey, string serviceName, object item);
    
    /// <summary>
    /// This service places a unique log service in [ StateTracker ] and in the [ Mongo ] database and the [ Log ]
    /// document
    /// </summary>
    /// <param name="uniqueKey"></param>
    /// <param name="serviceName"></param>
    /// <param name="item"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task RecordAsync(string uniqueKey, string serviceName, object item, 
        CancellationToken cancellationToken = default
    );
}