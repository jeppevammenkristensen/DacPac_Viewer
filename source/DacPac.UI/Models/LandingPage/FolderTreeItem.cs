using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Models.LandingPage;

public partial class FolderTreeItem : ObservableObject, IWrappingTreeItem<FolderTreeItem, ITreeItem>
{
    public ImmutableArray<ITreeItem> Items { get; }

    public FolderTreeItem(string title, IEnumerable<ITreeItem> items)
    {
        Name = title;
        Items = [.. items];
        Children = Items.ToList();
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
