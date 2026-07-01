using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace DacPac.UI.Infrastructure;

public class VelopackUpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/jeppevammenkristensen/DacPac_Viewer";

    private readonly ILogger<VelopackUpdateService> _logger;
    private readonly UpdateManager _updateManager;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateService(ILogger<VelopackUpdateService> logger)
    {
        _logger = logger;
        _updateManager = new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
    }

    public async Task<string?> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        // IsInstalled is false when not running from a Velopack install
        // (e.g. IDE or plain build output); update APIs would throw in that case.
        if (!_updateManager.IsInstalled)
        {
            _logger.LogInformation("Not a Velopack install; skipping update check");
            return null;
        }

        try
        {
            var update = await _updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                _logger.LogInformation("Application is up to date");
                return null;
            }

            await _updateManager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
            _pendingUpdate = update;
            return update.TargetFullRelease.Version.ToString();
        }
        catch (Exception ex)
        {
            // Update failures must never disturb normal application use
            _logger.LogWarning(ex, "Checking or downloading updates failed");
            return null;
        }
    }

    public void RestartAndApplyUpdate()
    {
        if (_pendingUpdate is null) return;
        _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
