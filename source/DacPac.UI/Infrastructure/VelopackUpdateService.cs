using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;
using Microsoft.Extensions.Logging;
using Velopack;
using Velopack.Sources;

namespace DacPac.UI.Infrastructure;

public partial class VelopackUpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/jeppevammenkristensen/DacPac_Viewer";

    private readonly ILogger<VelopackUpdateService> _logger;
    private readonly ISettingsService _settingsService;
    private readonly IMessenger _messenger;
    private UpdateManager? _lastUsedManager;
    private UpdateInfo? _pendingUpdate;

    public VelopackUpdateService(ILogger<VelopackUpdateService> logger, ISettingsService settingsService, IMessenger messenger)
    {
        _logger = logger;
        _settingsService = settingsService;
        _messenger = messenger;
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
            LogNotAVelopackInstallSkippingUpdateCheck();
            _messenger.Send(new StatusValueDataMessage(new StatusMessage("Not a Velopack install; skipping update check", StatusType.Info)));
            return null;
        }

        try
        {
            _messenger.Send(new StatusValueDataMessage(new StatusMessage("Checking for updates...", StatusType.Info)));
            var update = await updateManager.CheckForUpdatesAsync();
            if (update is null)
            {
                _messenger.Send(new StatusValueDataMessage(new StatusMessage("Application is up to date", StatusType.Info)));
                LogApplicationIsUpToDate();
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
            _messenger.Send(new StatusValueDataMessage(new StatusMessage("Failed to check or download updates", StatusType.Error)));
            LogCheckingOrDownloadingUpdatesFailed(ex);
            return null;
        }
    }

    public void RestartAndApplyUpdate()
    {
        if (_pendingUpdate is null || _lastUsedManager is null) return;
        _lastUsedManager.ApplyUpdatesAndRestart(_pendingUpdate);
    }

    [LoggerMessage(LogLevel.Information, "Not a Velopack install; skipping update check")]
    partial void LogNotAVelopackInstallSkippingUpdateCheck();

    [LoggerMessage(LogLevel.Information, "Application is up to date")]
    partial void LogApplicationIsUpToDate();

    [LoggerMessage(LogLevel.Warning, "Checking or downloading updates failed")]
    partial void LogCheckingOrDownloadingUpdatesFailed(Exception exception);
}
