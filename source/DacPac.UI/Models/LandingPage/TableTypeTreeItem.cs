using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.ViewModels.LandingPage;
using DacPac.UI.Views.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a user-defined table type in the landing page tree.
/// </summary>
public sealed partial class TableTypeTreeItem : ObservableObject, ISqlObjectRootTreeItem
{
    private readonly TSqlObject _source;
    private readonly IReadOnlyList<ITreeItem> _children;
    private readonly Dictionary<ObjectIdentifier, (SimpleTreeItem Procedure, List<ITreeItem> Parameters)> _procedures =
        new(new ObjectIdentifierComparer());

    public TableTypeTreeItem(TSqlObject source)
    {
        _source = source;
        _children = this.GetReferencedAndReferencing(TransformParameter).ToList();
    }

    /// <summary>
    /// Displays a table-type parameter beneath the procedure that owns it.
    /// </summary>
    private ITreeItem? TransformParameter(TSqlObject sqlObject)
    {
        if (sqlObject.ObjectType != Parameter.TypeClass)
            return null;

        var procedure = sqlObject.GetReferencing().FirstOrDefault(x => x.ObjectType == Procedure.TypeClass);
        if (procedure is null)
            return null;

        if (_procedures.TryGetValue(procedure.Name, out var procedureTree))
        {
            procedureTree.Parameters.Add(new SimpleTreeItem(sqlObject.Name.Parts.Last(), sqlObject.GetTreeIcon(), sqlObject));
            return procedureTree.Procedure;
        }

        var parameters = new List<ITreeItem>
        {
            new SimpleTreeItem(sqlObject.Name.Parts.Last(), sqlObject.GetTreeIcon(), sqlObject)
        };
        var procedureItem = new SimpleTreeItem(procedure.Name.Parts.Last(), procedure.GetTreeIcon(), procedure, parameters);
        _procedures.Add(procedure.Name, (procedureItem, parameters));
        return procedureItem;
    }

    public string Name => _source.Name.Parts.Last();
    public TSqlObject Source => _source;
    public string IconId => TreeIconIds.Table;
    public string ToolTip => $"Table type: {Name}";
    public IEnumerable<ITreeItem> Children => _children;

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    [ObservableProperty] public partial bool IsHidden { get; set; }

    [ObservableProperty] public partial bool IsMatch { get; set; }
}
