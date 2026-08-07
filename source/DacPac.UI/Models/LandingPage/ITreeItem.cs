using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

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
    public static void Traverse(this ITreeItem item, Action<ITreeItem> action)
    {
        action(item);
        foreach (var itemChild in item. Children)
        {
            Traverse(itemChild, action);
        }
    }
}