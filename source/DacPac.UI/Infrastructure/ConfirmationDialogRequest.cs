namespace DacPac.UI.Infrastructure;

/// <summary>
/// Describes the text and labels displayed by a confirmation prompt.
/// </summary>
public sealed record ConfirmationDialogRequest(
    string Title,
    string Message,
    string ConfirmText = "Confirm",
    string CancelText = "Cancel");
