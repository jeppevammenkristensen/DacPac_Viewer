using System;
using System.Collections.Generic;
using System.Linq;
using DacPac.Core;
using DacPac.UI.ViewModels.LandingPage;
using DacPac.UI.Views.LandingPage;
using Humanizer;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Provides helpers for reading metadata from DAC model objects.
/// </summary>
public static class SqlObjectExtensions
{
    /// <summary>
    /// Gets the schema referenced by the SQL object, when the object has a schema relationship.
    /// </summary>
    public static ObjectIdentifier? GetSchema(this TSqlObject source)
    {
        if (source.ObjectType.Relationships.FirstOrDefault(relationship => relationship.Name == "Schema") is not
            { } schemaRelationship)
            return null;

        return source.GetReferenced(schemaRelationship).FirstOrDefault()?.Name;
    }

    public static bool IsType(this TSqlObject source, ModelTypeClass typeName)
    {
        return source.ObjectType == typeName;
    }

    public static string? GetTreeIcon(this TSqlObject source)
    {
        if (source.IsType(Column.TypeClass))
            return TreeIconIds.Column;
        if (source.IsType(Schema.TypeClass))
            return TreeIconIds.Schema;
        if (source.IsType(Table.TypeClass))
            return TreeIconIds.Table;
        if (source.IsType(Procedure.TypeClass))
            return TreeIconIds.Procedure;
        if (source.IsType(Parameter.TypeClass))
            return TreeIconIds.Parameter;
        if (source.IsType(View.TypeClass))
            return TreeIconIds.View;

        return null;
    }

    public static IEnumerable<ITreeItem> GetReferencedAndReferencing(this ISqlObjectTreeItem source,
        params Func<TSqlObject, bool>[] predicates)
    {
        return GetFolderTreeItems("Referenced", source.GetReferencedTreeItems(predicates))
            .Concat(GetFolderTreeItems("Referenced by", source.GetReferencingTreeItems(predicates)));
    }

    private static IEnumerable<ITreeItem> GetReferencingTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        params Func<TSqlObject, bool>[] predicates)
    {
        var sqlObjects = sourceTreeItem.Source.GetReferencing();

        foreach (var predicate in predicates)
        {
            sqlObjects = sqlObjects.Where(predicate);
        }

        return GetTreeItems(sqlObjects);
    }

    private static IEnumerable<ITreeItem> GetReferencedTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        params Func<TSqlObject, bool>[] predicates)
    {
        var sqlObjects = sourceTreeItem.Source.GetReferenced();

        foreach (var predicate in predicates)
        {
            sqlObjects = sqlObjects.Where(predicate);
        }

        return GetTreeItems(sqlObjects);
    }

    private static IEnumerable<ITreeItem> GetTreeItems(IEnumerable<TSqlObject> sqlObjects)
    {
        foreach (var group in sqlObjects
                     .Where(x => x.Name.HasName)
                     .DistinctBy(x => x.Name, new ObjectIdentifierComparer())
                     .GroupBy(x => x.ObjectType.Name))
        {
            yield return new TypeGroupTreeItem(group.Key.Pluralize(), group.SkipNulls()
                .OrderBy(x => x.Name.Parts.Last()).Select(x =>
                    new SimpleTreeItem(x.Name.Parts.Last(),
                        x.GetTreeIcon(),
                        x)));
        }
    }

    private static IEnumerable<ITreeItem> GetFolderTreeItems(string name, IEnumerable<ITreeItem> items)
    {
        var children = items.OrderBy(x => x.Name).ToList();
        if (children.Count > 0)
            yield return new FolderTreeItem(name, children);
    }
}

public class TypeGroupTreeItem(string display, IEnumerable<ISqlObjectTreeItem> grouping) : ITreeItem
{
    public string Name { get; } = display;
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; }
    public IEnumerable<ITreeItem> Children { get; } = grouping.ToList();
}