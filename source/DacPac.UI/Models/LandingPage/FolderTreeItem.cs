using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a folder that groups related tree items.
/// </summary>
public partial class FolderTreeItem : ObservableObject, IWrappingTreeItem<FolderTreeItem, ITreeItem>
{
    /// <summary>Gets the items contained by the folder.</summary>
    public ImmutableArray<ITreeItem> Items { get; }

    public FolderTreeItem(string title, IEnumerable<ITreeItem> items)
    {
        Name = title;
        Items = [.. items];
        Children = Items.ToList();
    }

    /// <summary>Creates a folder tree item from child items.</summary>
    public static FolderTreeItem Create(string name, IEnumerable<ITreeItem> children)
    {
        return new FolderTreeItem(name, children);
    }

    /// <summary>Gets the folder name.</summary>
    public string Name { get; }
    /// <summary>Gets the folder icon identifier.</summary>
    public string? IconId { get; } = TreeIconIds.Folder;
    /// <summary>Gets the optional tooltip.</summary>
    public string? ToolTip { get; } = null;
    /// <summary>Gets the child tree items.</summary>
    public IEnumerable<ITreeItem> Children { get; }

    /// <summary>Gets or sets whether the item is expanded.</summary>
    [ObservableProperty] public partial bool IsExpanded { get; set; }

    /// <summary>Gets or sets whether the item is hidden.</summary>
    [ObservableProperty] public partial bool IsHidden { get; set; }

    /// <summary>Gets or sets whether the item matches the active search.</summary>
    [ObservableProperty] public partial bool IsMatch { get; set; }
}
