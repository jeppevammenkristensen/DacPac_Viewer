using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Persists small pieces of user-configurable application state across runs.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Whether the update check should include beta (pre-release) builds.
    /// Setting this value persists it immediately.
    /// </summary>
    bool EnableBetaUpdates { get; set; }
    
    void SaveOrUpdatePaths(IReadOnlyList<AbsolutePath> paths);
    IReadOnlyList<AbsolutePath[]> GetStoredPaths();
}
