using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using DacPac.UI.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure.Messages;
using DacPac.UI.ViewModels.LandingPage;
using DacPac.UI.ViewModels.Docker;
using DacPac.UI.ViewModels.ErrorHandling;
using DacPac.UI.ViewModels.Settings;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TruePath;

namespace DacPac.UI.ViewModels;

/// <summary>
/// Represents a group of dacpac files that was opened together.
/// </summary>
public sealed record RecentDacpacFiles(IReadOnlyList<AbsolutePath> Paths)
{
    /// <summary>
    /// Gets the filenames displayed for this recent entry.
    /// </summary>
    public string Title => string.Join(", ", Paths.Select(path => path.FileName));
}

/// <summary>
/// Represents an item in the Open menu, either the file picker or a recent entry.
/// </summary>
public sealed record OpenDacpacMenuItemData(RecentDacpacFiles? RecentFiles, string? ToolTip)
{
    /// <summary>
    /// Gets the text shown in the Open menu.
    /// </summary>
    public string Title => RecentFiles?.Title ?? "Open Dacpac";
}

[UsedImplicitly]
public partial class MainWindowViewModel : ViewModelBase,
    IRecipient<ProgressDataMessage>,
    IRecipient<InstallationRunningMessage>,
    IRecipient<StatusValueDataMessage>,
    IRecipient<StoredPathsChangedMessage>,
    IRecipient<OpenInstallationMessage>
{
    private readonly IServiceLocator _locator;
    private readonly IUpdateService _updateService;
    private readonly IApplicationInfoService _applicationInfoService;
    private readonly ISettingsService _settingsService;
    private readonly IFilePickerService _filePicker;

    public MainWindowViewModel(IServiceLocator locator,
        IUpdateService updateService,
        IApplicationInfoService applicationInfoService,
        ISettingsService settingsService,
        IFilePickerService filePicker,
        IErrorCollector errorCollector,
        NoScreensSelectedViewModel noScreensSelected)
    {
        _locator = locator;
        _updateService = updateService;
        _applicationInfoService = applicationInfoService;
        _settingsService = settingsService;
        _filePicker = filePicker;
        _ = errorCollector;
        NoScreensSelected = noScreensSelected;
        NoScreensSelected.SetMainWindowViewModel(this);
        Screens = [];
        Screens.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(DisplayPanel));
            OnPropertyChanged(nameof(DisplayHelp));
        };
        Status = string.Empty;
        Title = "DacPac viewer";
    }

    private bool CanExecuteOpenDacPac()
    {
        return true;
    }

    /// <summary>
    /// Prompts for dacpac files and loads the selected files on the landing page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteOpenDacPac))]
    private async Task OpenDacpac()
    {
        var files = await _filePicker.PickDacpacFilesAsync();
        if (files.Count == 0)
            return;

        var filesPaths = files.Select(AbsolutePath.Create).ToList();

        var landingPage = await GetLandingPage(files);
        await landingPage.OpenDacpacFilesAsync(filesPaths);
    }

    private async Task<LandingPageControlViewModel> GetLandingPage(IReadOnlyList<string> filesPaths)
    {
        var landingPageControlViewModel = Screens
            .OfType<LandingPageControlViewModel>()
            .FirstOrDefault(x => x.OpenedDacpacFiles.SequenceEqual(filesPaths));
        
        if (landingPageControlViewModel == null)
        {
            await LaunchPrimaryCommand.ExecuteAsync(null);
            var latestAdded = Screens.OfType<LandingPageControlViewModel>().Last();
            return latestAdded;
        }
        else
        {
            Screen = landingPageControlViewModel;
            return landingPageControlViewModel;
        }
    }

    private async Task LoadRecentDacpacs(RecentDacpacFiles recentFiles)
    {
        var landingPage = await GetLandingPage(recentFiles.Paths.Select(x => x.Value).ToList());
        await landingPage.OpenDacpacFilesAsync(recentFiles.Paths);
    }


    /// <summary>
    /// Gets the screens currently open in tabs.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(DisplayPanel))]
    [ObservableProperty]
    public partial ObservableCollection<IScreenPage> Screens { get; private set; }

    /// <summary>
    /// Gets the view model displayed when no screens are open.
    /// </summary>
    public NoScreensSelectedViewModel NoScreensSelected { get; }

    /// <summary>
    /// Gets the entries shown in the Open submenu.
    /// </summary>
    public ObservableCollection<object> OpenDacpacMenuItems { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected screen.
    /// </summary>
    [NotifyCanExecuteChangedFor(nameof(OpenDacpacMenuItemCommand))]
    [ObservableProperty]
    public partial IScreenPage? Screen { get; set; }

    /// <summary>
    /// Gets or sets the text displayed in the status area.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(IsStatusVisible))]
    [ObservableProperty]
    public partial string Status { get; set; }

    /// <summary>
    ///     The progress. Should be between 0 and 100.
    /// </summary>
    [ObservableProperty]
    public partial double CurrentProgress { get; set; }

    /// <summary>
    /// Gets whether a DacPac installation is in progress.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(IsProgressVisible))]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    [ObservableProperty]
    public partial bool IsInstalling { get; set; }

    /// <summary>
    /// Gets whether Velopack is checking for or downloading an application update.
    /// </summary>
    /// <summary>
    /// Gets or sets whether an application update is being checked for or downloaded.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(IsProgressVisible))]
    [NotifyPropertyChangedFor(nameof(IsProgressIndeterminate))]
    [ObservableProperty]
    private bool _isUpdating;

    /// <summary>
    /// Gets whether the shared progress indicator should be visible.
    /// </summary>
    public bool IsProgressVisible => IsInstalling || IsUpdating;

    /// <summary>
    /// Gets whether the shared progress indicator should use its indeterminate state.
    /// </summary>
    public bool IsProgressIndeterminate => IsInstalling || IsUpdating;

    /// <summary>
    /// Gets or sets whether startup initialization has completed.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(DisplayPanel))]
    [NotifyPropertyChangedFor(nameof(DisplayHelp))]
    [ObservableProperty]
    public partial bool Loaded { get; set; }

    /// <summary>
    /// Gets whether one or more screen tabs are open.
    /// </summary>
    public bool DisplayPanel => Loaded && Screens.Count > 0;

    /// <summary>
    /// Gets whether the empty-screen view should be displayed.
    /// </summary>
    public bool DisplayHelp => Loaded &&  Screens.Count == 0;

    /// <summary>
    /// Gets or sets whether a downloaded update is ready to install.
    /// </summary>
    [NotifyCanExecuteChangedFor(nameof(RestartAndUpdateCommand))]
    [ObservableProperty]
    public partial bool UpdateAvailable { get; set; }

    /// <summary>
    /// Gets or sets the main window title.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>
    /// Gets or sets the severity of the status message.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(DisplayInfo))]
    [NotifyPropertyChangedFor(nameof(DisplayInfoError))]
    [NotifyPropertyChangedFor(nameof(DisplaySuccess))]
    [ObservableProperty]
    public partial StatusType StatusType { get; set; }

    /// <summary>
    /// Gets or sets whether the dark theme is active.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(ThemeToggleGlyph))]
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; } = Application.Current?.ActualThemeVariant != ThemeVariant.Light;

    /// <summary>
    /// Gets whether the status message has informational severity.
    /// </summary>
    public bool DisplayInfo => StatusType == StatusType.Info;

    /// <summary>
    /// Gets whether the status message has error severity.
    /// </summary>
    public bool DisplayInfoError => StatusType == StatusType.Error;

    /// <summary>
    /// Gets whether the status message has success severity.
    /// </summary>
    public bool DisplaySuccess => StatusType == StatusType.Success;

    /// <summary>
    /// Gets whether the status area contains a message to display.
    /// </summary>
    public bool IsStatusVisible => !string.IsNullOrWhiteSpace(Status);

    /// <summary>
    /// Gets whether diagnostic controls should be shown.
    /// </summary>
    public bool IsDebugBuild =>
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>
    ///     Glyph shown on the theme toggle button, representing the theme that will be switched to.
    /// </summary>
    public string ThemeToggleGlyph => IsDarkTheme ? "☀" : "🌙";

    /// <summary>
    /// Gets or sets whether Docker is available on the current machine.
    /// </summary>
    [ObservableProperty]
    public partial bool DockerIsAvailable { get; set; }

    /// <summary>
    /// Updates the shared progress indicator.
    /// </summary>
    public void Receive(ProgressDataMessage message)
    {
        CurrentProgress = message.Value;
    }

    /// <summary>
    /// Updates the shared progress indicator while a DacPac installation runs.
    /// </summary>
    public void Receive(InstallationRunningMessage message)
    {
        IsInstalling = message.Value;
    }

    /// <summary>
    /// Updates the displayed status message.
    /// </summary>
    public void Receive(StatusValueDataMessage message)
    {
        Status = message.Value.Value;
        StatusType = message.Value.StatusType;
    }

    /// <summary>
    /// Bladi bladi blah
    /// </summary>
    /// <param name="token"></param>
    [RelayCommand]
    private async Task OnStartup(CancellationToken token)
    {
        OnActivated(); // hooks up implemented IRecipient

        CurrentProgress = 0;
        var longRunningTask = _locator.GetRequiredService<StartupTask>();
        IsInstalling = true;
        try
        {
            await longRunningTask.ExecuteTask(token);
        }
        finally
        {
            IsInstalling = false;
        }

        DockerIsAvailable = longRunningTask.DockerIsAvailable;
        Loaded = true;
        LoadRecentDacpacFiles();

        // Fire-and-forget; must never block or fail startup
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        IsUpdating = true;
        try
        {
            var version = await _updateService.CheckAndDownloadUpdateAsync();
            if (version is null) return;

            UpdateAvailable = true;
            Status = $"Version {version} has been downloaded. Restart to apply it.";
            StatusType = StatusType.Info;
        }
        finally
        {
            IsUpdating = false;
        }
    }

    /// <summary>
    /// Refreshes the recent dacpac menu after persisted paths change.
    /// </summary>
    public void Receive(StoredPathsChangedMessage message)
    {
        UpdateOpenDacpacMenuItems(message.Value);
    }

    private void UpdateOpenDacpacMenuItems(IEnumerable<AbsolutePath[]> files)
    {
        OpenDacpacMenuItems.Clear();
        OpenDacpacMenuItems.Add(new OpenDacpacMenuItemData(null, "Open one or more dac pac files"));

        foreach (var indexTuple in files.Index())
        {
            if (indexTuple.Index == 0)
            {
                OpenDacpacMenuItems.Add(new Separator());
            }

            OpenDacpacMenuItems.Add(new OpenDacpacMenuItemData(new RecentDacpacFiles(indexTuple.Item),
                string.Join(",", indexTuple.Item)));
        }
    }

    /// <summary>
    /// Refreshes the recent dacpac menu entries from persisted settings.
    /// </summary>
    private void LoadRecentDacpacFiles()
    {
        UpdateOpenDacpacMenuItems(_settingsService.GetStoredPaths());
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOpenDacPac))]
    private async Task OpenDacpacMenuItem(OpenDacpacMenuItemData menuItemData)
    {
        if (menuItemData.RecentFiles is null)
            await OpenDacpacCommand.ExecuteAsync(null);
        else
            await LoadRecentDacpacs(menuItemData.RecentFiles);
    }

    [RelayCommand(CanExecute = nameof(UpdateAvailable))]
    private void RestartAndUpdate()
    {
        _updateService.RestartAndApplyUpdate();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        Messenger.Send(new ThemeChangedMessage());
    }

    /// <summary>
    /// Shows the current application version in an About dialog.
    /// </summary>
    [RelayCommand]
    private void ShowAbout()
    {
        var owner =
            (Application.Current?.ApplicationLifetime as
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (owner is null) return;

        _ = new Views.AboutDialog(_applicationInfoService).ShowDialog(owner);
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
    }


    private bool CanExecuteLaunchPrimary()
    {
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteLaunchPrimary))]
    private async Task LaunchPrimary()
    {
        var screen = _locator.GetRequiredService<LandingPageControlViewModel>();
        await Launch(screen);
    }

    [RelayCommand]
    private async Task LaunchSettings()
    {
        var screen = _locator.GetRequiredService<SettingsPageViewModel>();
        await Launch(screen);
    }

    [RelayCommand]
    private async Task LaunchSqlServerSetup()
    {
        var screen = _locator.GetRequiredService<SqlServerSetupPageViewModel>();
        await Launch(screen);
    }

    [RelayCommand]
    private async Task LaunchReportBug()
    {
        ClearStatus();
        var screen = _locator.GetRequiredService<ReportBugViewModel>();
        await Launch(screen);
    }

    /// <summary>
    /// Hides the current status message without removing collected exceptions.
    /// </summary>
    [RelayCommand]
    private void ClearStatus()
    {
        Status = string.Empty;
    }

    [RelayCommand]
    private void TriggerTestError()
    {
        Messenger.SendException("This is a test error for verifying bug reporting.",
            new InvalidOperationException("Test error triggered from the Help menu."));
    }

    partial void OnScreenChanged(IScreenPage? oldValue, IScreenPage? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ScreenPropertyChanged;

        if (newValue is not null) newValue.PropertyChanged += ScreenPropertyChanged;
    }

    private void ScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Screen.CanClose)) CloseCommand.NotifyCanExecuteChanged();
    }

    private async Task Launch(IScreenPage screenPage)
    {
        Screens.Add(screenPage);
        await screenPage.OnActivatedAsync();
        Screen = screenPage;
    }

    /// <summary>
    /// Opens a screen page in a new tab and selects it.
    /// </summary>
    public Task LaunchScreenAsync(ScreenPage screenPage)
    {
        return Launch(screenPage);
    }

    private bool CanExecuteClose(IScreenPage? screen)
    {
        if (screen is null) return false;

        return screen.CanClose;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteClose))]
    private async Task Close(IScreenPage screenPage)
    {
        await screenPage.CloseAsync();
        Screens.Remove(screenPage);
        if (Screens.Count > 0)
            Screen = Screens[^1];
        else
            Screen = null;
    }

    /// <summary>
    /// Opens an installation screen for the requested packages.
    /// </summary>
    public void Receive(OpenInstallationMessage message)
    {
        message.Reply(LaunchInstallation(message.Paths));
    }

    private async Task<bool> LaunchInstallation(AbsolutePath[] paths)
    {
        var installationViewModel = _locator.GetRequiredService<InstallationViewModel>();
        installationViewModel.SetPackages(paths);
        await LaunchScreenAsync(installationViewModel);
        return true;
    }
}
