using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Microsoft.Extensions.Configuration;

namespace DacPac.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<ProgressDataMessage>, IRecipient<StatusValueDataMessage>
{
    private readonly IServiceLocator _locator;
    private readonly IUpdateService _updateService;

    public MainWindowViewModel(IServiceLocator locator, IConfiguration configuration, IUpdateService updateService)
    {
        _locator = locator;
        _updateService = updateService;
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


    [ObservableProperty] public partial ObservableCollection<ScreenPage> Screens { get; set; }

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
