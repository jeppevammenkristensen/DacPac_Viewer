using System.ComponentModel;
using System.Text;
using FileBasedApp.Toolkit.SimpleExec;
using Microsoft.Extensions.Logging;

namespace DacPac.Core;

/// <summary>
/// Executes Docker CLI commands used by the application.
/// </summary>
public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;

    public DockerService(ILogger<DockerService> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Determines whether the Docker daemon is available.
    /// </summary>
    public async Task<bool> PingDocker()
    {
        try
        {
            var readAsync = await SimpleExecRunner.Init("docker")
                .AddArgument("info")
                .AddArgumentPair("--format", "{{.ServerVersion}}")
                .ReadAsync();
            return !string.IsNullOrWhiteSpace(readAsync.StandardOutput);
        }
        catch (SimpleExec.ExitCodeReadException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Enumerates all containers reported by Docker.
    /// </summary>
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
