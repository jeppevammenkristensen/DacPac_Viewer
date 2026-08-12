using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Displays common information for SQL objects without a specialized view model.
/// </summary>
public class DefaultDisplayViewModel : DisplayViewModel
{
    /// <summary>
    /// Initializes a default display for a SQL object.
    /// </summary>
    public DefaultDisplayViewModel(TSqlObject model) : base(model)
    {
    }
}