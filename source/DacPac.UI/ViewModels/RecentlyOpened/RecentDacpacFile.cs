using TruePath;

namespace DacPac.UI.ViewModels.RecentlyOpened;

/// <summary>
/// Describes the current availability of a remembered DacPac file.
/// </summary>
public sealed record RecentDacpacFile(AbsolutePath Path, bool IsAvailable)
{
    /// <summary>
    /// Gets whether the file is unavailable at its remembered path.
    /// </summary>
    public bool IsUnavailable => !IsAvailable;

    /// <summary>
    /// Gets the availability text shown in the tooltip.
    /// </summary>
    public string AvailabilityToolTip => IsAvailable
        ? "File is available."
        : "File was not found at this path.";
}
