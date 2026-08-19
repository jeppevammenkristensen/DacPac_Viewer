using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Views.LandingPage;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a table in the SQL object tree.
/// </summary>
public sealed partial class TableTreeItem : ObservableObject, ISqlObjectRootTreeItem
{
    public readonly TableWrapper _tableWrapper;
    private readonly IReadOnlyList<ITreeItem> _children;

    public TableTreeItem(TSqlObject source)
    {
        _tableWrapper = source.ToTable();
        _children = this.GetReferencedAndReferencing().ToList();
    }

    /// <summary>Gets the table name.</summary>
    public string Name => _tableWrapper.SqlObject.Name.Parts.Last();
    /// <summary>Gets the underlying DacPac object.</summary>
    public TSqlObject Source => _tableWrapper.SqlObject;
    /// <summary>Gets the table icon identifier.</summary>
    public string IconId => TreeIconIds.Table;
    /// <summary>Gets the table tooltip.</summary>
    public string ToolTip => $"Table: {Name}";

    /// <summary>Gets the child tree items.</summary>
    public IEnumerable<ITreeItem> Children => _children;

    [ObservableProperty]
    /// <summary>Gets or sets whether the item is expanded.</summary>
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    /// <summary>Gets or sets whether the item is hidden.</summary>
    public partial bool IsHidden { get; set; }

    [ObservableProperty]
    /// <summary>Gets or sets whether the item matches the active search.</summary>
    public partial bool IsMatch { get; set; }
}
