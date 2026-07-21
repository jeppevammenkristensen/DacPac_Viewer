using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels.Displays;

namespace DacPac.UI.Views.Displays;

public partial class TableDisplay : UserControl
{
    public TableDisplay()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is TableDisplayViewModel viewModel)
        {
            ScriptEditor.Text = viewModel.Script;
            ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, System.EventArgs e)
        => ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
}
