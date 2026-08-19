using System.ComponentModel;
using System.Threading.Tasks;

namespace DacPac.UI.Infrastructure;

public interface IScreenPage : INotifyPropertyChanged
{
    /// <summary>
    /// Override this to perform an operation after an instance of the given screen page had been activated.
    /// </summary>
    Task OnActivatedAsync();

    /// <summary>
    /// Signals whether the current screen can close.
    /// </summary>
    bool CanClose { get; set; }

    string Title { get; }
    Task CloseAsync();
}
