using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DacPac.UI.Models.LandingPage;
using DacPac.UI.ViewModels.Displays;
using DacPac.UI.ViewModels.LandingPage;

namespace DacPac.UI.Views.LandingPage;

public partial class LandingPageControl : UserControl
{
    public LandingPageControl()
    {
        InitializeComponent();
        ResultsGrid.KeyDown += ResultsGridOnKeyDown;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LandingPageControlViewModel viewModel)
        {
            viewModel.ObjectTree = ObjectTree;
        }
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
                CollapseAllForItem(container);
            }
        }
    }


    private static void CollapseAllForItem(TreeViewItem item)
    {
        foreach (var child in item.GetRealizedContainers().OfType<TreeViewItem>())
        {
            CollapseAllForItem(child);
        }

        item.IsExpanded = false;
    }

    public static TreeViewItem? FindContainer(ItemsControl parent, object selectedItem)
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
