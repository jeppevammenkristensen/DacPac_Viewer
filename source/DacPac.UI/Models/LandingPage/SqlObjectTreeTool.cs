using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.ViewModels.LandingPage;
using DacPac.UI.Views.LandingPage;
using Humanizer;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Provides helpers for reading metadata from DAC model objects.
/// </summary>
public static class SqlObjectTreeTool
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
        if (source.IsType(TableType.TypeClass))
            return TreeIconIds.Table;

        return null;
    }

    public static IEnumerable<ITreeItem> GetReferencedAndReferencing(this ISqlObjectTreeItem source,
        params Func<TSqlObject, bool>[] predicates)
    {
        return GetFolderTreeItems("Referenced", source.GetReferencedTreeItems(predicates))
            .Concat(GetFolderTreeItems("Referenced by", source.GetReferencingTreeItems(predicates)));
    }

    /// <summary>
    /// Gets referenced and referencing objects, allowing selected objects to use a specialized tree representation.
    /// </summary>
    public static IEnumerable<ITreeItem> GetReferencedAndReferencing(this ISqlObjectTreeItem source,
        Func<TSqlObject, ITreeItem?> treeItemTransform, params Func<TSqlObject, bool>[] predicates)
    {
        return GetFolderTreeItems("Referenced", source.GetReferencedTreeItems(treeItemTransform, predicates))
            .Concat(GetFolderTreeItems("Referenced by", source.GetReferencingTreeItems(treeItemTransform, predicates)));
    }

    public static IEnumerable<ITreeItem> GetReferencingTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        params Func<TSqlObject, bool>[] predicates)
    {
        return sourceTreeItem.GetReferencingTreeItems(null, predicates);
    }

    /// <summary>
    /// Gets tree items for objects that reference the source, applying an optional specialized representation.
    /// </summary>
    public static IEnumerable<ITreeItem> GetReferencingTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        Func<TSqlObject, ITreeItem?>? treeItemTransform, params Func<TSqlObject, bool>[] predicates)
    {
        var sqlObjects = sourceTreeItem.Source.GetReferencing()
            .Where(sqlObject => sqlObject.ObjectType != Schema.TypeClass);

        foreach (var predicate in predicates)
        {
            sqlObjects = sqlObjects.Where(predicate);
        }

        return GetTreeItems(sqlObjects, treeItemTransform);
    }

    /// <summary>
    /// Wraps one tree item in the specified wrapper type.
    /// </summary>
    public static TWrappingItem WrapIn<TWrappingItem, TTreeItem>(this TTreeItem item, string name)
        where TWrappingItem : IWrappingTreeItem<TWrappingItem, TTreeItem> where TTreeItem : ITreeItem
    {
        return TWrappingItem.Create(name, [item]);
    }

    /// <summary>
    /// Wraps tree items in the specified wrapper type.
    /// </summary>
    public static TWrappingItem WrapIn<TWrappingItem, TTreeItem>(this IEnumerable<TTreeItem> items, string name)
        where TWrappingItem : IWrappingTreeItem<TWrappingItem, TTreeItem> where TTreeItem : ITreeItem
    {
        return TWrappingItem.Create(name, items);
    }

    private static IEnumerable<ITreeItem> GetReferencedTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        params Func<TSqlObject, bool>[] predicates)
    {
        return sourceTreeItem.GetReferencedTreeItems(null, predicates);
    }

    /// <summary>
    /// Gets tree items for objects referenced by the source, applying an optional specialized representation.
    /// </summary>
    private static IEnumerable<ITreeItem> GetReferencedTreeItems(this ISqlObjectTreeItem sourceTreeItem,
        Func<TSqlObject, ITreeItem?>? treeItemTransform, params Func<TSqlObject, bool>[] predicates)
    {
        var sqlObjects = sourceTreeItem.Source.GetReferenced()
            .Where(sqlObject => sqlObject.ObjectType != Schema.TypeClass);

        foreach (var predicate in predicates)
        {
            sqlObjects = sqlObjects.Where(predicate);
        }

        return GetTreeItems(sqlObjects, treeItemTransform);
    }

    /// <summary>
    /// Groups SQL objects by the type displayed in the tree, falling back to a simple item when no transformation applies.
    /// </summary>
    private static IEnumerable<ITreeItem> GetTreeItems(IEnumerable<TSqlObject> sqlObjects,
        Func<TSqlObject, ITreeItem?>? treeItemTransform)
    {
        var treeItems = sqlObjects
            .Where(x => x.Name.HasName)
            .DistinctBy(x => x.Name, new ObjectIdentifierComparer())
            .Select(sqlObject => (SqlObject: sqlObject,
                TreeItem: treeItemTransform?.Invoke(sqlObject) ?? new SimpleTreeItem(sqlObject.Name.Parts.Last(),
                    sqlObject.GetTreeIcon(), sqlObject)))
            .DistinctBy(x => (x.TreeItem as ISqlObjectTreeItem)?.Source.Name ?? x.SqlObject.Name,
                new ObjectIdentifierComparer());

        foreach (var group in treeItems.GroupBy(x => (x.TreeItem as ISqlObjectTreeItem)?.Source.ObjectType.Name
                                                     ?? x.SqlObject.ObjectType.Name))
        {
            yield return new TypeGroupTreeItem(group.Key.Pluralize(), group
                .OrderBy(x => x.TreeItem.Name)
                .Select(x => x.TreeItem));
        }
    }

    public static IEnumerable<ITreeItem> GetFolderTreeItems(string name, IEnumerable<ITreeItem> items)
    {
        var children = items.OrderBy(x => x.Name).ToList();
        if (children.Count > 0)
            yield return new FolderTreeItem(name, children);
    }
}

public partial class TypeGroupTreeItem(string display, IEnumerable<ITreeItem> grouping)
    : ObservableObject, IWrappingTreeItem<TypeGroupTreeItem, ITreeItem>
{
    public static TypeGroupTreeItem Create(string name, IEnumerable<ITreeItem> children)
    {
        return new TypeGroupTreeItem(name, children);
    }

    public string Name { get; } = display;
    public string? IconId { get; } = TreeIconIds.Folder;
    public string? ToolTip { get; } = null;
    public IEnumerable<ITreeItem> Children { get; } = grouping.ToList();

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    [ObservableProperty] public partial bool IsHidden { get; set; }

    [ObservableProperty] public partial bool IsMatch { get; set; }
}
