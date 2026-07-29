using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure;

namespace DacPac.UI.ViewModels.Settings;

public partial class SettingsPageViewModel(
    ISettingsService settingsService,
    IFileLocations fileLocations,
    IFolderLauncher folderLauncher) : ScreenPage
{
    public override string Title => "Settings";

    [ObservableProperty] public partial bool EnableBetaUpdates { get; set; } = settingsService.EnableBetaUpdates;


    [ObservableProperty]
    public partial bool PersistConnectionStrings { get; set; } = settingsService.StoreConnectionStrings;

    partial void OnEnableBetaUpdatesChanged(bool value)
    {
        settingsService.EnableBetaUpdates = value;
    }

    partial void OnPersistConnectionStringsChanged(bool value)
    {
        settingsService.StoreConnectionStrings = value;
    }

    /// <summary>
    /// Opens the folder containing persisted application data.
    /// </summary>
    [RelayCommand]
    private void OpenDataFolder()
    {
        folderLauncher.Open(fileLocations.RootSaveLocation);
    }
}