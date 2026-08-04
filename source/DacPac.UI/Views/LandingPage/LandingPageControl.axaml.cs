using Avalonia.Controls;
using Avalonia.Input;

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
}
