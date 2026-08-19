using Avalonia.Controls;
using DacPac.UI.Infrastructure;

namespace DacPac.UI.Views;

/// <summary>
/// Modal window that presents a reusable confirmation prompt.
/// </summary>
public partial class ConfirmationDialog : Window
{
    /// <summary>
    /// Initializes a confirmation dialog using the supplied request text.
    /// </summary>
    public ConfirmationDialog(ConfirmationDialogRequest request)
    {
        InitializeComponent();
        DataContext = new ConfirmationDialogViewModel(request, result => Close(result));
    }
}
