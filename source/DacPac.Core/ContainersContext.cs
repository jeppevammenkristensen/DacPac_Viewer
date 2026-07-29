using System.Text.Json.Serialization;

namespace DacPac.Core;

[JsonSerializable(typeof(Containers))]
[JsonSerializable(typeof(List<Containers>))]
public partial class ContainersContext : JsonSerializerContext
{
    
}