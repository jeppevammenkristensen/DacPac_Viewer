using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Views.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a stored procedure in the landing page tree.
/// </summary>
public sealed partial class ProcedureTreeItem : ObservableObject, ISqlObjectTreeItem
{
    private readonly TSqlObject _source;
    private readonly IReadOnlyList<ITreeItem> _children;

    public ProcedureTreeItem(TSqlObject source)
    {
        _source = source;
        _children = this.GetReferencedAndReferencing(x => x.ObjectType != DataType.TypeClass).ToList();
    }

    public string Name => _source.Name.Parts.Last();
    public TSqlObject Source => _source;
    public string IconId => TreeIconIds.Procedure;
    public string ToolTip => $"Procedure: {Name}";
    public IEnumerable<ITreeItem> Children => _children;
    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    [ObservableProperty]
    public partial bool IsMatch { get; set; }
}
