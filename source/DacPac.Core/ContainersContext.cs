using System.Text.Json.Serialization;

namespace DacPac.Core;

[JsonSerializable(typeof(Containers))]
[JsonSerializable(typeof(List<Containers>))]
/// <summary>
/// Provides source-generated JSON serialization metadata for Docker container data.
/// </summary>
public partial class ContainersContext : JsonSerializerContext
{
    
}
