using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Default <see cref="IClipboardService"/> backed by the desktop main window's clipboard.
/// </summary>
public class TopLevelClipboardService : IClipboardService
{
    public Task SetTextAsync(string text)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow.Clipboard: { } clipboard
            })
        {
            return Task.CompletedTask;
        }

        return clipboard.SetTextAsync(text);
    }
}
