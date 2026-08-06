using System.Collections.Generic;
using System.Linq;
using DacPac.UI.Views.LandingPage;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

public class FolderTreeItem : ITreeItem
{
    public IEnumerable<ITreeItem> Items { get; }

    public FolderTreeItem(string title, IEnumerable<ITreeItem> items)
    {
        Name = title;
        Children = items;
    }

    public string Name { get; }
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; } = null;
    public IEnumerable<ITreeItem> Children { get; }
}

public sealed class TableTreeItem : ISqlObjectTreeItem
{
    public readonly TableWrapper _tableWrapper;

    public TableTreeItem(TSqlObject source)
    {
        _tableWrapper = source.ToTable();
    }

    public string Name => _tableWrapper.SqlObject.Name.Parts.Last();
    public TSqlObject Source => _tableWrapper.SqlObject;
    public string IconId => TreeIconIds.Table;
    public string ToolTip => $"Table: {Name}";

    public IEnumerable<ITreeItem> Children => this.GetReferencedAndReferencing();
}