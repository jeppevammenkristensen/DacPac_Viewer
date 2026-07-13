using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.ApplicationLayer.Infrastructure;

namespace DacPac.UI.ViewModels.Settings;

public partial class SettingsPageViewModel(ISettingsService settingsService) : ScreenPage
{
    public override string Title => "Settings";

    [ObservableProperty] public partial bool EnableBetaUpdates { get; set; } = settingsService.EnableBetaUpdates;

    partial void OnEnableBetaUpdatesChanged(bool value)
    {
        settingsService.EnableBetaUpdates = value;
    }
}
