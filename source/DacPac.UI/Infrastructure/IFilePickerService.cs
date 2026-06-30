using System.Collections.Generic;
using System.Threading.Tasks;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Abstracts the platform file picker so view models can request files without
/// depending on a <see cref="Avalonia.Controls.TopLevel"/> or window.
/// </summary>
public interface IFilePickerService
{
    /// <summary>
    /// Opens a multi-select file dialog filtered to <c>*.dacpac</c> files.
    /// </summary>
    /// <returns>The selected local file paths, or an empty list if cancelled.</returns>
    Task<IReadOnlyList<string>> PickDacpacFilesAsync();
}
