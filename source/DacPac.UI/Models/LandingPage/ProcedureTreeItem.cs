using System.Collections.Generic;
using System.Linq;
using DacPac.UI.Views.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a stored procedure in the landing page tree.
/// </summary>
public sealed class ProcedureTreeItem : ISqlObjectTreeItem
{
    private readonly TSqlObject _source;

    public ProcedureTreeItem(TSqlObject source)
    {
        _source = source;
    }

    public string Name => _source.Name.Parts.Last();
    public TSqlObject Source => _source;
    public string IconId => TreeIconIds.Procedure;
    public string ToolTip => $"Procedure: {Name}";
    public IEnumerable<ITreeItem> Children => this.GetReferencedAndReferencing(x => x.ObjectType != DataType.TypeClass);
}