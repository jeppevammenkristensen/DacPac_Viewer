namespace DacPac.UI.ViewModels;

/// <summary>
/// Represents the command for opening the recently opened files management page.
/// </summary>
public sealed record ManageRecentlyOpenedMenuItemData
{
    /// <summary>
    /// Gets the text shown in the Open menu.
    /// </summary>
    public string Title => "Manage Recently Opened...";
}