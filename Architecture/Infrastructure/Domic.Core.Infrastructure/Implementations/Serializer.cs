using Domic.Core.Domain.Contracts.Interfaces;
using Newtonsoft.Json;

namespace Domic.Core.Infrastructure.Implementations;

public class Serializer : ISerializer
{
    public string Serialize<T>(T @object) 
        => JsonConvert.SerializeObject(@object, Formatting.Indented);
    
    public T DeSerialize<T>(string source) 
        => JsonConvert.DeserializeObject<T>(source, new JsonSerializerSettings { Formatting = Formatting.Indented});
}