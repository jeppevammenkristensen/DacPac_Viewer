using System.Collections.Generic;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Defines a tree item that can wrap child items under a named node.
/// </summary>
public interface IWrappingTreeItem<TSelf, TTreeItem> : ITreeItem
    where TSelf : IWrappingTreeItem<TSelf, TTreeItem> where TTreeItem : ITreeItem
{
    /// <summary>
    /// Creates a wrapping tree item with the supplied name and children.
    /// </summary>
    static abstract TSelf Create(string name, IEnumerable<TTreeItem> children);
}
