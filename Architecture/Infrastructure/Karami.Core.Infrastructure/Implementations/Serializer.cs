﻿using Karami.Core.Domain.Contracts.Interfaces;
using Newtonsoft.Json;

namespace Karami.Core.Domain.Implementations;

public class Serializer : ISerializer
{
    public string Serialize<T>(T @object) 
        => JsonConvert.SerializeObject(@object, Formatting.Indented);
    
    public T DeSerialize<T>(string source) 
        => JsonConvert.DeserializeObject<T>(source, new JsonSerializerSettings { Formatting = Formatting.Indented});
}