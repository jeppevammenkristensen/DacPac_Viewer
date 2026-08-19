using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a database schema and its supported objects in the landing page tree.
/// </summary>
public sealed partial class SchemaTreeItem : ObservableObject, ITreeItem
{
    private readonly IReadOnlyList<ITreeItem> _children;
    public readonly ObjectIdentifier Identifier;

    public SchemaTreeItem(ObjectIdentifier source, IEnumerable<TSqlObject> children)
    {
        Identifier = source;
        _children = GetChildren(children).ToList();
    }

    /// <summary>Gets the schema name.</summary>
    public string Name => Identifier.Parts.Last();
    /// <summary>Gets the schema icon identifier.</summary>
    public string IconId => TreeIconIds.Schema;
    /// <summary>Gets the schema tooltip.</summary>
    public string ToolTip => $"Schema: {Name}";

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

    private static IEnumerable<ITreeItem> GetChildren(IEnumerable<TSqlObject> children)
    {
        foreach (var sqlObject in children.GroupBy(x => x.ObjectType.Name))
        {
            var modelTypeClass = sqlObject.First().ObjectType;
            if (modelTypeClass == Table.TypeClass)
            {
                var tableTreeItems = sqlObject.Select(x => new TableTreeItem(x));
                yield return new FolderTreeItem("Tables", tableTreeItems);
            }
            else if (modelTypeClass == View.TypeClass)
            {
                var viewTreeItems = sqlObject.Select(x => new ViewTreeItem(x));
                yield return new FolderTreeItem("Views", viewTreeItems);
            }
            else if (modelTypeClass == Procedure.TypeClass)
            {
                yield return new FolderTreeItem("Procedures", sqlObject.Select(x => new ProcedureTreeItem(x)));
            }
            else if (modelTypeClass == TableType.TypeClass)
            {
                yield return new FolderTreeItem("Table Types", sqlObject.Select(x => new TableTypeTreeItem(x)));
            }
        }
    }
}
