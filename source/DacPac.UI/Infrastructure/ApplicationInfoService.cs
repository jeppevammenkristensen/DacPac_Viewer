using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Resolves version and release metadata for both Velopack installs and development builds.
/// </summary>
public sealed class ApplicationInfoService : IApplicationInfoService
{
    private const string RepositoryUrl = "https://github.com/jeppevammenkristensen/DacPac_Viewer";

    /// <inheritdoc />
    public string Version { get; } = GetVersion();

    /// <inheritdoc />
    public System.Uri ReleaseUri => new($"{RepositoryUrl}/releases/tag/v{Version}");

    /// <summary>
    /// Prefers Velopack package metadata and falls back to MinVer's assembly metadata for development builds.
    /// </summary>
    private static string GetVersion()
    {
        var updateManager = new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
        var velopackVersion = updateManager.IsInstalled
            ? updateManager.CurrentVersion?.ToString()
            : null;

        if (!string.IsNullOrWhiteSpace(velopackVersion)) return velopackVersion;

        var assembly = Assembly.GetEntryAssembly();
        return assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly?.GetName().Version?.ToString()
               ?? "Unknown";
    }
}
