using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DacPac.UI.Infrastructure;

namespace DacPac.UI.ViewModels;

/// <summary>
/// Supplies the version text shown by <see cref="AboutDialog"/>.
/// </summary>
public sealed class AboutDialogViewModel
{
    /// <summary>
    /// Initializes the About dialog content.
    /// </summary>
    public AboutDialogViewModel(IApplicationInfoService applicationInfoService)
    {
        Version = $"Version {applicationInfoService.Version}";
        ReleaseUri = applicationInfoService.ReleaseUri;
        OpenReleaseNotesCommand = new RelayCommand(OpenReleaseNotes);
    }

    /// <summary>
    /// Gets the formatted application version.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the link to the matching GitHub release.
    /// </summary>
    public System.Uri ReleaseUri { get; }

    /// <summary>
    /// Gets the command that opens the GitHub release page.
    /// </summary>
    public ICommand OpenReleaseNotesCommand { get; }

    /// <summary>
    /// Opens the GitHub release page in the operating system's default browser.
    /// </summary>
    private void OpenReleaseNotes()
    {
        Process.Start(new ProcessStartInfo(ReleaseUri.AbsoluteUri) { UseShellExecute = true });
    }
}