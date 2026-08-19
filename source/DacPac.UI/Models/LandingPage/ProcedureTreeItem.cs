using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Views.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a stored procedure in the landing page tree.
/// </summary>
public sealed partial class ProcedureTreeItem : ObservableObject, ISqlObjectRootTreeItem
{
    private readonly TSqlObject _source;
    private readonly IReadOnlyList<ITreeItem> _children;

    public ProcedureTreeItem(TSqlObject source)
    {
        _source = source;
        _children = this.GetReferencedAndReferencing(x => x.ObjectType != DataType.TypeClass).ToList();
    }

    /// <summary>Gets the procedure name.</summary>
    public string Name => _source.Name.Parts.Last();
    /// <summary>Gets the underlying DacPac object.</summary>
    public TSqlObject Source => _source;
    /// <summary>Gets the procedure icon identifier.</summary>
    public string IconId => TreeIconIds.Procedure;
    /// <summary>Gets the procedure tooltip.</summary>
    public string ToolTip => $"Procedure: {Name}";
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
