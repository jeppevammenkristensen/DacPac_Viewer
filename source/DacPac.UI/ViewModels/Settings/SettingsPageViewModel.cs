using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.ApplicationLayer.Infrastructure;

namespace DacPac.UI.ViewModels.Settings;

public partial class SettingsPageViewModel(ISettingsService settingsService) : ScreenPage
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
}
