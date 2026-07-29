using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.Core;

namespace DacPac.UI.Infrastructure.LongRunning;

// NOTE: This is a dummy task to demonstrate a long-running operation that reports progress and status.
public class StartupTask(IMessenger messenger, IDockerService service) : BaseProgressReportingTask(messenger)
{
    private readonly IDockerService _service = service;

    public bool DockerIsAvailable {get; private set; }
    
    public override async Task ExecuteTask(CancellationToken? token)
    {
        ReportStatus("Starting engines... (DummyTask)");

        ReportStatus("Testing if docker is available");

        DockerIsAvailable = await _service.PingDocker();

        ReportStatus($"Engines started");
    }
}