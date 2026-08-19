using System.Windows.Input;

namespace DacPac.UI.ViewModels;

public interface ICommandButton
{
    /// <summary>
    /// Gets the text displayed on the button.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the command executed when the button is clicked.
    /// </summary>
    ICommand RunCommand { get; }

    /// <summary>
    /// Gets whether the button is displayed.
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// Gets the button tooltip.
    /// </summary>
    string? ToolTip { get; }

    /// <summary>
    /// Gets whether the button uses the accent style.
    /// </summary>
    bool IsAccent { get; }
}
