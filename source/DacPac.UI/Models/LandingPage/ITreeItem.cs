using System.Collections.Generic;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents an item displayed in the landing page tree.
/// </summary>
public interface ITreeItem
{
    /// <summary>
    /// Gets the text displayed for this item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the optional identifier of the icon displayed beside this item.
    /// </summary>
    string? IconId { get; }

    /// <summary>
    /// Gets the optional text displayed when the pointer hovers over this item.
    /// </summary>
    string? ToolTip { get; }

    /// <summary>
    /// Gets the child items displayed beneath this item.
    /// </summary>
    IEnumerable<ITreeItem> Children { get; }

    /// <summary>
    /// Gets or sets whether the item's child nodes are displayed.
    /// </summary>
    bool IsExpanded { get; set; }

    bool IsHidden { get; set; }

    bool IsMatch { get; set; }
}