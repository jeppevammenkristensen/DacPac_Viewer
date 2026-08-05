using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a stored procedure in the landing page tree.
/// </summary>
public sealed class ProcedureTreeItem : ITreeItem
{
    private readonly TSqlObject _source;

    public ProcedureTreeItem(TSqlObject source)
    {
        _source = source;
    }

    public string Name => _source.Name.Parts.Last();
    public string IconId => "Procedure";
    public string ToolTip => $"Procedure: {Name}";
    public IEnumerable<ITreeItem> Children => [];
}