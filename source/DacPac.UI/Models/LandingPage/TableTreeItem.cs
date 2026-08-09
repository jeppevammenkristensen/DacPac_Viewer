using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Views.LandingPage;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

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
