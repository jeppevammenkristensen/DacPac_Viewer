using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Provides tab-screen behavior with data-annotation validation support.
/// </summary>
public abstract class ValidatingScreenPage : ObservableValidator, IScreenPage
{
    /// <summary>
    /// Gets the title displayed in the tab view.
    /// </summary>
    public abstract string Title { get; }

    public virtual Task CloseAsync() => Task.CompletedTask;

    public virtual Task OnActivatedAsync() => Task.CompletedTask;
    public virtual bool CanClose { get; set; } = true;
}
