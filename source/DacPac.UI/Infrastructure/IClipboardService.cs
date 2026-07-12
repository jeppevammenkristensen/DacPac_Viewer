using System.Threading.Tasks;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Abstracts the platform clipboard so view models can copy text without
/// depending on a <see cref="Avalonia.Controls.TopLevel"/> or window.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies the given text to the system clipboard.
    /// </summary>
    Task SetTextAsync(string text);
}
