using System;
using System.Collections.Immutable;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Provides traversal helpers for landing page tree items.
/// </summary>
public static class TreeItemExtensions
{
    public static void Traverse(this ITreeItem item, ImmutableArray<ITreeItem>? parents,
        Action<ITreeItem, ImmutableArray<ITreeItem>> action)
    {
        var tree = parents ?? ImmutableArray<ITreeItem>.Empty;

        action(item, tree);
        tree = tree.Add(item);

        foreach (var itemChild in item.Children)
        {
            Traverse(itemChild, tree, action);
        }
    }

    public static void Traverse(this ITreeItem item, Action<ITreeItem> action)
    {
        action(item);
        foreach (var itemChild in item.Children)
        {
            Traverse(itemChild, action);
        }
    }
}