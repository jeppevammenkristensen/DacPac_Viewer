namespace DacPac.Core;

public interface IDockerService
{
    IAsyncEnumerable<Containers> ListContainers();
    Task<bool> PingDocker();
}