using System.Threading.Tasks;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Displays modal confirmation prompts without exposing view models to Avalonia window APIs.
/// </summary>
public interface IConfirmationDialogService
{
    /// <summary>
    /// Shows a modal confirmation dialog and returns whether the user confirmed the action.
    /// </summary>
    Task<bool> ConfirmAsync(ConfirmationDialogRequest request);
}
