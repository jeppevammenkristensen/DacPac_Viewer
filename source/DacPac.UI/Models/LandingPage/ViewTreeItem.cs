using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a database view in the landing page tree.
/// </summary>
public sealed class ViewTreeItem : ITreeItem
{
    private readonly TSqlObject _source;

    public ViewTreeItem(TSqlObject source)
    {
        _source = source;
    }

    public string Name => _source.Name.Parts.Last();
    public string IconId => "View";
    public string ToolTip => $"View: {Name}";
    public IEnumerable<ITreeItem> Children => [];
}