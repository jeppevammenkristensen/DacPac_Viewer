using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.Core;
using DacPac.UI.Infrastructure;
using FileBasedApp.Toolkit.SimpleExec;

namespace DacPac.UI.ViewModels.Docker;

/// <summary>
/// Provides the input fields and future actions for a local Docker SQL Server setup.
/// </summary>
public partial class SqlServerSetupPageViewModel(IDockerService dockerService, IMessenger messenger) : ScreenPage
{
    /// <summary>
    /// Gets the title displayed by the setup tab.
    /// </summary>
    public override string Title => "Docker SQL Server";

    [ObservableProperty] public partial string ContainerName { get; set; } = "dacpac-sqlserver";

    [ObservableProperty] public partial string HostPort { get; set; } = "1433";

    [ObservableProperty] public partial string SaPassword { get; set; } = "YourStrong!Password123";

    [ObservableProperty] public partial bool IsPasswordVisible { get; set; }

    [ObservableProperty] public partial bool PersistData { get; set; } = true;

    /// <summary>
    /// Records that the container creation workflow has not yet been implemented.
    /// </summary>
    [RelayCommand]
    private async Task StartContainer()
    {
        var containersList = await dockerService.ListContainers().ToListAsync();
        if (containersList.Any(x => x.Names.Contains(ContainerName)))
        {
            messenger.SendInformation("Container with the same name already exists.");
            return;
        }

        if (containersList.Any(x => HasPublishedHostPort(x.Ports, HostPort)))
        {
            messenger.SendInformation("Port already in use.");
            return;
        }

        await SimpleExecRunner.Init("docker")
            .AddArgument("run")
            .AddArgumentPair("--name", ContainerName)
            .AddArgumentPair("-e", "ACCEPT_EULA=Y")
            .AddArgumentPair("-e", "MSSQL_SA_PASSWORD=YourStrong!Password123")
            .AddArgumentPair("-p", $"{HostPort}:1433")
            .AddArgumentPair("-v", $"{ContainerName} not connected yet.")
            .AddArgumentPair("-d", "mcr.microsoft.com/mssql/server:2022-latest")
            .WithEchoPrefix("Docker-Setup")
            .RunAsync();
        
        messenger.SendSuccess("Container created successfully.");
    }

    /// <summary>
    /// Determines whether Docker has published the specified host port in a container's port mapping.
    /// </summary>
    private static bool HasPublishedHostPort(string ports, string hostPort)
    {
        return DockerPublishedPortRegex()
            .Matches(ports)
            .Any(match => match.Groups["port"].Value == hostPort);
    }

    [GeneratedRegex(@"(?:^|,\s*)(?:\[[^\]]+\]|[^,:]+):(?<port>\d+)->", RegexOptions.CultureInvariant)]
    private static partial Regex DockerPublishedPortRegex();
}
