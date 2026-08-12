using Avalonia.Controls;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels;

namespace DacPac.UI.Views;

/// <summary>
/// Displays basic application information.
/// </summary>
public partial class AboutDialog : Window
{
    /// <summary>
    /// Initializes the dialog with the running application's release metadata.
    /// </summary>
    public AboutDialog(IApplicationInfoService applicationInfoService)
    {
        InitializeComponent();
        DataContext = new AboutDialogViewModel(applicationInfoService);
    }

    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}