using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.Core;

namespace DacPac.UI.Infrastructure.LongRunning;

/// <summary>
/// Checks Docker availability during application startup.
/// </summary>
public class StartupTask(IMessenger messenger, IDockerService service) : BaseProgressReportingTask(messenger)
{
    private readonly IDockerService _service = service;

    /// <summary>
    /// Gets whether Docker was available when startup completed.
    /// </summary>
    public bool DockerIsAvailable {get; private set; }
    
    /// <summary>
    /// Tests Docker availability and reports startup status.
    /// </summary>
    public override async Task ExecuteTask(CancellationToken? token)
    {
        ReportStatus("Starting engines...");
        ReportStatus("Testing if docker is available");

        DockerIsAvailable = await _service.PingDocker();

        ReportStatus($"Engines started");
    }
}
