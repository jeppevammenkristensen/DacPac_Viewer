using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TruePath;

namespace DacPac.UI.ViewModels.RecentlyOpened;

/// <summary>
/// Represents one remembered DacPac open operation.
/// </summary>
public partial class RecentDacpacEntry : ObservableObject
{
    /// <summary>
    /// Initializes a remembered open operation and its file availability details.
    /// </summary>
    public RecentDacpacEntry(IReadOnlyList<AbsolutePath> paths, IFileSystem fileSystem)
    {
        Paths = paths;
        Files = paths.Select(path => new RecentDacpacFile(path, path.FileExists(fileSystem))).ToList();
    }

    /// <summary>
    /// Gets the paths that comprise this open operation.
    /// </summary>
    public IReadOnlyList<AbsolutePath> Paths { get; }

    /// <summary>
    /// Gets the files displayed for this entry.
    /// </summary>
    public IReadOnlyList<RecentDacpacFile> Files { get; }

    /// <summary>
    /// Gets or sets whether this entry is selected for removal.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
