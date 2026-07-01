using System.Threading;
using System.Threading.Tasks;

namespace DacPac.UI.Infrastructure;

public interface IUpdateService
{
    /// <summary>
    ///     Checks the release feed for a newer version and downloads it if found.
    ///     Returns the new version number, or null if up to date (or updates are
    ///     not supported, e.g. when running from the IDE).
    /// </summary>
    Task<string?> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Applies a previously downloaded update and restarts the application.
    ///     No-op if no update has been downloaded.
    /// </summary>
    void RestartAndApplyUpdate();
}
