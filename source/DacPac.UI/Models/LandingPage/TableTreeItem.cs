using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Views.LandingPage;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

public partial class FolderTreeItem : ObservableObject, ITreeItem
{
    public IEnumerable<ITreeItem> Items { get; }

    public FolderTreeItem(string title, IEnumerable<ITreeItem> items)
    {
        Name = title;
        Children = items.ToList();
    }

    public string Name { get; }
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; } = null;
    public IEnumerable<ITreeItem> Children { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    [ObservableProperty]
    public partial bool IsMatch { get; set; }
}

public sealed partial class TableTreeItem : ObservableObject, ISqlObjectTreeItem
{
    public readonly TableWrapper _tableWrapper;
    private readonly IReadOnlyList<ITreeItem> _children;

    public TableTreeItem(TSqlObject source)
    {
        _tableWrapper = source.ToTable();
        _children = this.GetReferencedAndReferencing().ToList();
    }

    public string Name => _tableWrapper.SqlObject.Name.Parts.Last();
    public TSqlObject Source => _tableWrapper.SqlObject;
    public string IconId => TreeIconIds.Table;
    public string ToolTip => $"Table: {Name}";

    public IEnumerable<ITreeItem> Children => _children;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    [ObservableProperty]
    public partial bool IsMatch { get; set; }
}
