using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Defines a tree item that can wrap child items under a named node.
/// </summary>
public interface IWrappingTreeItem<TSelf, TTreeItem> : ITreeItem
    where TSelf : IWrappingTreeItem<TSelf, TTreeItem> where TTreeItem : ITreeItem
{
    /// <summary>
    /// Creates a wrapping tree item with the supplied name and children.
    /// </summary>
    static abstract TSelf Create(string name, IEnumerable<TTreeItem> children);
}

public partial class FolderTreeItem : ObservableObject, IWrappingTreeItem<FolderTreeItem, ITreeItem>
{
    public IEnumerable<ITreeItem> Items { get; }

    public FolderTreeItem(string title, IEnumerable<ITreeItem> items)
    {
        Name = title;
        Items = items.ToList();
        Children = Items;
    }

    public static FolderTreeItem Create(string name, IEnumerable<ITreeItem> children)
    {
        return new FolderTreeItem(name, children);
    }

    public string Name { get; }
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; } = null;
    public IEnumerable<ITreeItem> Children { get; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    [ObservableProperty] public partial bool IsHidden { get; set; }

    [ObservableProperty] public partial bool IsMatch { get; set; }
}