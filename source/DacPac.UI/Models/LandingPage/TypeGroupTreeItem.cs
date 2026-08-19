using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Models.LandingPage;

public partial class TypeGroupTreeItem(string display, IEnumerable<ITreeItem> grouping)
    : ObservableObject, IWrappingTreeItem<TypeGroupTreeItem, ITreeItem>
{
    public static TypeGroupTreeItem Create(string name, IEnumerable<ITreeItem> children)
    {
        return new TypeGroupTreeItem(name, children);
    }

    public string Name { get; } = display;
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; } = null;
    public IEnumerable<ITreeItem> Children { get; } = grouping.ToList();

    [ObservableProperty] public partial bool IsExpanded { get; set; }
    [ObservableProperty] public partial bool IsHidden { get; set; }
    [ObservableProperty] public partial bool IsMatch { get; set; }
}
