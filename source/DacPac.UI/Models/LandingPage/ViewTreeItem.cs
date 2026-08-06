using System.Collections.Generic;
using System.Linq;
using DacPac.UI.Views.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a database view in the landing page tree.
/// </summary>
public sealed class ViewTreeItem : ISqlObjectTreeItem
{
    private readonly TSqlObject _source;

    public ViewTreeItem(TSqlObject source)
    {
        _source = source;
    }

    public string Name => _source.Name.Parts.Last();
    public TSqlObject Source => _source;
    public string IconId => TreeIconIds.View;
    public string ToolTip => $"View: {Name}";

    public IEnumerable<ITreeItem> Children => this.GetReferencedAndReferencing();
}