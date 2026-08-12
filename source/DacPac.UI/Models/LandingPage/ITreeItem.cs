using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// A marker interface for the upper level tree item
/// </summary>
/// <remarks>This is used when searching to identify root items.</remarks>
public interface ISqlObjectRootTreeItem : ISqlObjectTreeItem
{
    
}

public interface ISqlObjectTreeItem : ITreeItem
{
    public TSqlObject Source { get; }
}

/// <summary>
/// Represents an item displayed in the landing page tree.
/// </summary>
public interface ITreeItem
{
    /// <summary>
    /// Gets the text displayed for this item.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the optional identifier of the icon displayed beside this item.
    /// </summary>
    string? IconId { get; }

    /// <summary>
    /// Gets the optional text displayed when the pointer hovers over this item.
    /// </summary>
    string? ToolTip { get; }

    /// <summary>
    /// Gets the child items displayed beneath this item.
    /// </summary>
    IEnumerable<ITreeItem> Children { get; }

    /// <summary>
    /// Gets or sets whether the item's child nodes are displayed.
    /// </summary>
    bool IsExpanded { get; set; }

    bool IsHidden { get; set; }
    
    bool IsMatch { get; set; }
}

public static class Extensions
{
    public static void Traverse(this ITreeItem item, ImmutableArray<ITreeItem>? parents, Action<ITreeItem, ImmutableArray<ITreeItem>> action)
    {
        var tree = parents ?? ImmutableArray<ITreeItem>.Empty;

        action(item, tree);
        tree = tree.Add(item);
        
        foreach (var itemChild in item. Children)
        {
            Traverse(itemChild, tree, action);
        }
    }
    
    
    
    public static void Traverse(this ITreeItem item, Action<ITreeItem> action)
    {
        action(item);
        foreach (var itemChild in item. Children)
        {
            Traverse(itemChild, action);
        }
    }
}