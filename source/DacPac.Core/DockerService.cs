using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using FileBasedApp.Toolkit.SimpleExec;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.Extensions.Logging;

namespace DacPac.Core;

public interface IDockerService
{
    IAsyncEnumerable<Containers> ListContainers();
    Task<bool> PingDocker();
}

public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
    }
    
    public async Task<bool> PingDocker()
    {
        try
        {
            var readAsync = await SimpleExecRunner.Init("docker").AddArgument("--version")
                .ReadAsync();
            if (readAsync.StandardError is {Length: > 0})
            {
                return false;
            }

            return true;
        }
        catch (SimpleExec.ExitCodeReadException)
        {
            return false;
        }
    }
    
    public async IAsyncEnumerable<Containers> ListContainers()
    {
        var readAsync = await SimpleExecRunner.Init("docker").AddArgument("ps")
            .AddArgument("-a")
            .AddArgumentPair("--format", "json")
            .WithEncoding(Encoding.UTF8)
            .ReadAsync();
        
        var stringReader = new StringReader(readAsync.StandardOutput);

        while (stringReader.Peek() > -1)
        {
            var currentLine = await stringReader.ReadLineAsync();
            if (currentLine is not null)
            {
                var content = System.Text.Json.JsonSerializer.Deserialize(currentLine,ContainersContext.Default.Containers);
                if (content != null)
                {
                    yield return content;
                }
            }
            
        }
    }
}

[JsonSerializable(typeof(Containers))]
[JsonSerializable(typeof(List<Containers>))]
public partial class ContainersContext : JsonSerializerContext
{
    
}

public record Containers(
    string Command,
    string CreatedAt,
    string HealthStatus,
    string ID,
    string Image,
    string Labels,
    string LocalVolumes,
    string Mounts,
    string Names,
    string Networks,
    object Platform,
    string Ports,
    string RunningFor,
    string Size,
    string State,
    string Status
);
