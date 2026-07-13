using System;
using System.Threading;
using System.Threading.Tasks;
using DacPac.UI.ApplicationLayer.Infrastructure;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace DacPac.UI.Infrastructure;

public class VelopackUpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/jeppevammenkristensen/DacPac_Viewer";

    private readonly ILogger<VelopackUpdateService> _logger;
    private readonly ISettingsService _settingsService;
    private UpdateManager? _lastUsedManager;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateService(ILogger<VelopackUpdateService> logger, ISettingsService settingsService)
    {
        _logger = logger;
        _settingsService = settingsService;
    }

    // Built fresh on every check so a change to EnableBetaUpdates takes effect
    // on the next check without requiring an app restart.
    private UpdateManager CreateUpdateManager()
    {
        return new UpdateManager(new GithubSource(RepositoryUrl, accessToken: null, prerelease: _settingsService.EnableBetaUpdates));
    }

    public async Task<string?> CheckAndDownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        var updateManager = CreateUpdateManager();

        // IsInstalled is false when not running from a Velopack install
        // (e.g. IDE or plain build output); update APIs would throw in that case.
        if (!updateManager.IsInstalled)
        {
            _logger.LogInformation("Not a Velopack install; skipping update check");
            return null;
        }

        try
        {
            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                _logger.LogInformation("Application is up to date");
                return null;
            }

            await updateManager.DownloadUpdatesAsync(update, cancelToken: cancellationToken);
            _lastUsedManager = updateManager;
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
        if (_pendingUpdate is null || _lastUsedManager is null) return;
        _lastUsedManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }
}
