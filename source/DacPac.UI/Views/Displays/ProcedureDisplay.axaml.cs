using Avalonia.Controls;
using DacPac.UI.Infrastructure;
using DacPac.UI.ViewModels.LandingPage.Displays;

namespace DacPac.UI.Views.Displays;

public partial class ProcedureDisplay : UserControl
{
    public ProcedureDisplay()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        ActualThemeVariantChanged += OnActualThemeVariantChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ProcedureDisplayViewModel viewModel)
        {
            ScriptEditor.Text = viewModel.Script;
            ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, System.EventArgs e)
        => ScriptEditor.SyntaxHighlighting = CodeSyntaxHighlighting.Sql;
}
