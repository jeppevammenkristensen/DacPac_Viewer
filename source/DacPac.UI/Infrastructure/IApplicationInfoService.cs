namespace DacPac.UI.Infrastructure;

/// <summary>
/// Provides metadata describing the running application and its release.
/// </summary>
public interface IApplicationInfoService
{
    /// <summary>
    /// Gets the version displayed to the user.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the GitHub page for the running version's release.
    /// </summary>
    System.Uri ReleaseUri { get; }
}
