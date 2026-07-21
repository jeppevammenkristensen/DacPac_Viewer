using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using DacPac.UI.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;
using DacPac.UI.ViewModels.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.ApplicationLayer.Infrastructure;
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

public partial class MainWindowViewModel : ViewModelBase, IRecipient<ProgressDataMessage>, IRecipient<StatusValueDataMessage>
{
    private readonly IServiceLocator _locator;
    private readonly IUpdateService _updateService;
    private readonly ISettingsService _settingsService;

    public MainWindowViewModel(IServiceLocator locator, IUpdateService updateService, ISettingsService settingsService)
    {
        _locator = locator;
        _updateService = updateService;
        _settingsService = settingsService;
        Screens = [];
        Status = string.Empty;
        Title = "DacPac viewer";
    }

    private bool CanExecuteOpenDacPac()
    {
        return Screen is LandingPageControlViewModel;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOpenDacPac))]
    private async Task OpenDacPac()
    {
        if (Screen is LandingPageControlViewModel landingPage)
        {
            await landingPage.OpenDacpacCommand.ExecuteAsync(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOpenDacPac))]
    private async Task LoadRecentDacpacs(RecentDacpacFiles recentFiles)
    {
        if (Screen is not LandingPageControlViewModel landingPage)
            return;

        await landingPage.OpenDacpacFilesAsync(recentFiles.Paths);
        LoadRecentDacpacFiles();
    }


    [ObservableProperty] public partial ObservableCollection<ScreenPage> Screens { get; set; }

    /// <summary>
    /// Gets the dacpac file groups available from previous open operations.
    /// </summary>
    public ObservableCollection<RecentDacpacFiles> RecentDacpacFiles { get; } = [];

    [NotifyCanExecuteChangedFor(nameof(OpenDacPacCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoadRecentDacpacsCommand))]
    [ObservableProperty] public partial ScreenPage? Screen { get; set; }

    [ObservableProperty] public partial string Status { get; set; }

    /// <summary>
    ///     The progress. Should be between 0 and 100.
    /// </summary>
    [ObservableProperty]
    public partial double CurrentProgress { get; set; }

    [ObservableProperty] public partial bool Loaded { get; set; }

    [NotifyCanExecuteChangedFor(nameof(RestartAndUpdateCommand))]
    [ObservableProperty]
    public partial bool UpdateAvailable { get; set; }

    [ObservableProperty] public partial string Title { get; set; }
    [NotifyPropertyChangedFor(nameof(DisplayInfo))] [NotifyPropertyChangedFor(nameof(DisplayInfoError))] [ObservableProperty] public partial StatusType StatusType { get; set; }

    [NotifyPropertyChangedFor(nameof(ThemeToggleGlyph))]
    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; } = Application.Current?.ActualThemeVariant != ThemeVariant.Light;

    public bool DisplayInfo => StatusType == StatusType.Info;
    public bool DisplayInfoError => StatusType == StatusType.Error;

    /// <summary>
    ///     Glyph shown on the theme toggle button, representing the theme that will be switched to.
    /// </summary>
    public string ThemeToggleGlyph => IsDarkTheme ? "☀" : "🌙";

    public void Receive(ProgressDataMessage message)
    {
        CurrentProgress = message.Value;
    }

    public void Receive(StatusValueDataMessage message)
    {
        Status = message.Value.Value;
        StatusType = message.Value.StatusType;
        
    }

    [RelayCommand]
    private async Task OnStartup(CancellationToken token)
    {
        OnActivated(); // hooks up implemented IRecipient

        CurrentProgress = 0;
        var longRunningTask = new DummyTask(Messenger);
        await longRunningTask.ExecuteTask(token);
        Loaded = true;
        await LaunchPrimaryCommand.ExecuteAsync(null);
        LoadRecentDacpacFiles();

        // Fire-and-forget; must never block or fail startup
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        var version = await _updateService.CheckAndDownloadUpdateAsync();
        if (version is null) return;

        UpdateAvailable = true;
        Status = $"Version {version} has been downloaded. Restart to apply it.";
        StatusType = StatusType.Info;
    }

    /// <summary>
    /// Refreshes the recent dacpac menu entries from persisted settings.
    /// </summary>
    private void LoadRecentDacpacFiles()
    {
        RecentDacpacFiles.Clear();
        foreach (var paths in _settingsService.GetStoredPaths())
            RecentDacpacFiles.Add(new RecentDacpacFiles(paths));
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

    partial void OnScreenChanged(ScreenPage? oldValue, ScreenPage? newValue)
    {
        if (oldValue is not null) oldValue.PropertyChanged -= ScreenPropertyChanged;

        if (newValue is not null) newValue.PropertyChanged += ScreenPropertyChanged;
    }

    private void ScreenPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Screen.CanClose)) CloseCommand.NotifyCanExecuteChanged();
    }

    private async Task Launch(ScreenPage screenPage)
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

    private bool CanExecuteClose(ScreenPage? screen)
    {
        if (screen is null) return false;

        return screen.CanClose;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteClose))]
    private async Task Close(ScreenPage screenPage)
    {
        await screenPage.CloseAsync();
        Screens.Remove(screenPage);
        if (Screens.Count > 0)
            Screen = Screens[^1];
        else
            Screen = null;
    }
}
