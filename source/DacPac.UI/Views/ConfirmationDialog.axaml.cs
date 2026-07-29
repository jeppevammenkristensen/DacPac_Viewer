using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

/// <summary>
/// Supplies display text and actions for <see cref="ConfirmationDialog"/>.
/// </summary>
public sealed partial class ConfirmationDialogViewModel : ObservableObject
{
    private readonly System.Action<bool> _close;

    /// <summary>
    /// Initializes the dialog's display text and result handler.
    /// </summary>
    public ConfirmationDialogViewModel(ConfirmationDialogRequest request, System.Action<bool> close)
    {
        Title = request.Title;
        Message = request.Message;
        ConfirmText = request.ConfirmText;
        CancelText = request.CancelText;
        _close = close;
    }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmText { get; }

    public string CancelText { get; }

    [RelayCommand]
    private void Confirm() => _close(true);

    [RelayCommand]
    private void Cancel() => _close(false);
}
