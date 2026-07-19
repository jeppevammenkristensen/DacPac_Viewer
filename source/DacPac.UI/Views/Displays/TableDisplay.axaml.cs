using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Highlighting;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels.Displays;

namespace DacPac.UI.Views.Displays;

public partial class TableDisplay : UserControl
{
    public TableDisplay()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TableDisplayViewModel viewModel)
        {
            ScriptEditor.Text = viewModel.Script;
            ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.For(viewModel.Script);
        }
    }
}
