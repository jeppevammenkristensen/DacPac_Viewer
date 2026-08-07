using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Models.LandingPage;
using DacPac.UI.ViewModels.Displays;
using DacPac.UI.ViewModels.LandingPage;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Views.LandingPage;

public partial class LandingPageControl : UserControl
{
    public LandingPageControl()
    {
        InitializeComponent();
        ResultsGrid.KeyDown += ResultsGridOnKeyDown;
    }

    private void ClearComboBoxSelection(object? sender, SelectionChangedEventArgs e)
    {
        // This is done to enforce that the Placeholder text (for instance 1 selected) is "always" displayed

        if (sender is ComboBox {SelectedIndex: not -1} comboBox)
        {
            comboBox.Clear();
        }
    }

    private void ResultsGridOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e is {Key: Key.A, KeyModifiers: KeyModifiers.Control})
        {
            ResultsGrid.SelectAll();
            e.Handled = true;
        }
    }

    private void TreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.DataContext is LandingPageControlViewModel viewModel &&
            e.AddedItems.OfType<ITreeItem>().FirstOrDefault() is ISqlObjectTreeItem treeItem)
        {
            viewModel.SetDetails(treeItem.Source);
        }
    }

    private void ExpandAllTreeItems(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var item in ObjectTree.ItemsSource?.OfType<ITreeItem>() ?? [])
        {
            ExpandAll(ObjectTree, item, full: false);
        }
    }

    private static void ExpandAll(ItemsControl parent, ITreeItem item, bool full)
    {
        if (FindContainer(parent, item) is { } container)
        {
            container.IsExpanded = true;
        }

        foreach (var child in item.Children)
        {
            if (child is not ISqlObjectTreeItem || full)
            {
                ExpandAll(parent, child, full);
            }
        }
    }

    private void ExpandSelected(object? sender, RoutedEventArgs e)
    {
        var tree = (TreeView)sender!;
        var selectedItems = tree.SelectedItems;     
        
        foreach (var selectedItem in ObjectTree.SelectedItems)
        {
            var container = FindContainer(ObjectTree, selectedItem);
            if (container is not null && selectedItem is ITreeItem treeItem)
            {
                ExpandAll(ObjectTree, treeItem, full: true);
            }
        }
    }

    private void CollapseSelected(object? sender, RoutedEventArgs e)
    {
        foreach (var selectedItem in ObjectTree.SelectedItems)
        {
            var container = FindContainer(ObjectTree, selectedItem);
            if (container is not null)
            {
                CollapseAll(container);
            }
        }
    }

    private static void CollapseAll(TreeViewItem item)
    {
        foreach (var child in item.GetRealizedContainers().OfType<TreeViewItem>())
        {
            CollapseAll(child);
        }

        item.IsExpanded = false;
    }

    private static TreeViewItem? FindContainer(ItemsControl parent, object selectedItem)
    {
        foreach (var container in parent.GetRealizedContainers().OfType<TreeViewItem>())
        {
            if (ReferenceEquals(container.DataContext, selectedItem))
                return container;

            var nested = FindContainer(container, selectedItem);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}

public partial class SimpleTreeItem : ObservableObject, ISqlObjectTreeItem
{
    public SimpleTreeItem(string name, string? iconId, TSqlObject obj)
    {
        Name = name;
        IconId = iconId;
        ToolTip = obj.ObjectType.Name;
        Source = obj;
    }

    public string Name { get; }
    public string? IconId { get; }
    public string? ToolTip { get; }
    public IEnumerable<ITreeItem> Children { get; } = [];
    public TSqlObject Source { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial bool IsHidden { get; set; }

    [ObservableProperty]
    public partial bool IsMatch { get; set; }
}
