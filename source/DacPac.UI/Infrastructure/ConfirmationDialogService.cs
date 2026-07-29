using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DacPac.UI.Views;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Displays confirmation requests in a modal Avalonia window owned by the main window.
/// </summary>
public class ConfirmationDialogService : IConfirmationDialogService
{
    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(ConfirmationDialogRequest request)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow: { } owner
            })
        {
            return false;
        }

        var dialog = new ConfirmationDialog(request);
        return await dialog.ShowDialog<bool>(owner);
    }
}
