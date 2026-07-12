using Avalonia.Controls;
using Avalonia.Input;

namespace DacPac.UI.Views;

public partial class LandingPageControl : UserControl
{
    public LandingPageControl()
    {
        InitializeComponent();
        ResultsGrid.KeyDown += ResultsGridOnKeyDown;
    }

    private void ResultsGridOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
        {
            ResultsGrid.SelectAll();
            e.Handled = true;
        }
    }
}
