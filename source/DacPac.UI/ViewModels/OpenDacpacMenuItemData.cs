namespace DacPac.UI.ViewModels;

/// <summary>
/// Represents an item in the Open menu, either the file picker or a recent entry.
/// </summary>
public sealed record OpenDacpacMenuItemData(RecentDacpacFiles? RecentFiles, string? ToolTip)
{
    /// <summary>
    /// Gets the text shown in the Open menu.
    /// </summary>
    public string Title => RecentFiles?.Title ?? "Open Dacpac";
}
