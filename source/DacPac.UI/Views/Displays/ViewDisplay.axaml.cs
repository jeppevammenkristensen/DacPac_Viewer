using Avalonia.Controls;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels.Displays;

namespace DacPac.UI.Views.Displays;

public partial class ViewDisplay : UserControl
{
    public ViewDisplay()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ViewDisplayViewModel viewModel)
        {
            ScriptEditor.Text = viewModel.Script;
            ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, System.EventArgs e)
        => ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
}
