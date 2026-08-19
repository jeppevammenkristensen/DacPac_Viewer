using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Groups SQL object tree items of the same type.
/// </summary>
public partial class TypeGroupTreeItem(string display, IEnumerable<ITreeItem> grouping)
    : ObservableObject, IWrappingTreeItem<TypeGroupTreeItem, ITreeItem>
{
    /// <summary>Creates a type group from child items.</summary>
    public static TypeGroupTreeItem Create(string name, IEnumerable<ITreeItem> children)
    {
        return new TypeGroupTreeItem(name, children);
    }

    /// <summary>Gets the group name.</summary>
    public string Name { get; } = display;
    /// <summary>Gets the group icon identifier.</summary>
    public string? IconId { get; } = TreeIconIds.Folder;
    /// <summary>Gets the optional tooltip.</summary>
    public string? ToolTip { get; } = null;
    /// <summary>Gets the child tree items.</summary>
    public IEnumerable<ITreeItem> Children { get; } = grouping.ToList();

    /// <summary>Gets or sets whether the item is expanded.</summary>
    [ObservableProperty] public partial bool IsExpanded { get; set; }
    /// <summary>Gets or sets whether the item is hidden.</summary>
    [ObservableProperty] public partial bool IsHidden { get; set; }
    /// <summary>Gets or sets whether the item matches the active search.</summary>
    [ObservableProperty] public partial bool IsMatch { get; set; }
}
