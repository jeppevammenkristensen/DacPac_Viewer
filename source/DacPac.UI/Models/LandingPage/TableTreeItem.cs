using System.Collections.Generic;
using System.Linq;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

public sealed class TableTreeItem : ITreeItem
{
    private readonly TableWrapper _tableWrapper;

    public TableTreeItem(TSqlObject source)
    {
        _tableWrapper = source.ToTable();
    }

    public string Name => _tableWrapper.SqlObject.Name.Parts.Last();
    public string IconId => "Table";
    public string ToolTip => $"Table: {Name}";

    public IEnumerable<ITreeItem> Children => [];
}
