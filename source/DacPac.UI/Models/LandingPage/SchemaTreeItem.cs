using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Represents a database schema and its supported objects in the landing page tree.
/// </summary>
public sealed class SchemaTreeItem : ITreeItem
{
    private readonly IEnumerable<TSqlObject> _children;
    private readonly ObjectIdentifier _schemaWrapper;

    public SchemaTreeItem(ObjectIdentifier source, IEnumerable<TSqlObject> children)
    {
        _children = children;
        _schemaWrapper = source;
    }

    public string Name => _schemaWrapper.Parts.Last();
    public string IconId => "Schema";
    public string ToolTip => $"Schema: {Name}";

    public IEnumerable<ITreeItem> Children => GetChildren();

    private IEnumerable<ITreeItem> GetChildren()
    {
        foreach (var sqlObject in _children)
        {
            if (sqlObject.ObjectType == Table.TypeClass)
            {
                yield return new TableTreeItem(sqlObject);
            }
            else if (sqlObject.ObjectType == View.TypeClass)
            {
                yield return new ViewTreeItem(sqlObject);
            }
            else if (sqlObject.ObjectType == Procedure.TypeClass)
            {
                yield return new ProcedureTreeItem(sqlObject);
            }
        }
    }
}