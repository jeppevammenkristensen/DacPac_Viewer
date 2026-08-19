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

    /// <summary>Gets the table-type name.</summary>
    public string Name => _source.Name.Parts.Last();
    /// <summary>Gets the underlying DacPac object.</summary>
    public TSqlObject Source => _source;
    /// <summary>Gets the table-type icon identifier.</summary>
    public string IconId => TreeIconIds.Table;
    /// <summary>Gets the table-type tooltip.</summary>
    public string ToolTip => $"Table type: {Name}";
    /// <summary>Gets the child tree items.</summary>
    public IEnumerable<ITreeItem> Children => _children;

    /// <summary>Gets or sets whether the item is expanded.</summary>
    [ObservableProperty] public partial bool IsExpanded { get; set; }

    /// <summary>Gets or sets whether the item is hidden.</summary>
    [ObservableProperty] public partial bool IsHidden { get; set; }

    /// <summary>Gets or sets whether the item matches the active search.</summary>
    [ObservableProperty] public partial bool IsMatch { get; set; }
}
