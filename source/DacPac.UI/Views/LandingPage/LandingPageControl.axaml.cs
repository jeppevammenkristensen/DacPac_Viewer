using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
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
        if (this.DataContext is LandingPageControlViewModel viewModel && e.AddedItems.OfType<ITreeItem>().FirstOrDefault() is ISqlObjectTreeItem treeItem)
        {
            viewModel.SetDetails(treeItem.Source);
        }
    }
}

public class SimpleTreeItem : ISqlObjectTreeItem
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
}
