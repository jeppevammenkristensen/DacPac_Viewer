using System.Collections.Generic;
using System.Linq;
using TruePath;

namespace DacPac.UI.ViewModels;

/// <summary>
/// Represents a group of DacPac files that was opened together.
/// </summary>
public sealed record RecentDacpacFiles(IReadOnlyList<AbsolutePath> Paths)
{
    /// <summary>
    /// Gets the filenames displayed for this recent entry.
    /// </summary>
    public string Title => string.Join(", ", Paths.Select(path => path.FileName));
}
