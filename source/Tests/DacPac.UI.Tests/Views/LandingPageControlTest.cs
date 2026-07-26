using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DacPac.UI.Views;
using Xunit;

namespace DacPac.UI.Tests.Views;

public class LandingPageControlTest
{
    [AvaloniaFact]
    public void InitializesResultsGrid()
    {
        var view = new LandingPageControl();

        Assert.NotNull(view.FindControl<DataGrid>("ResultsGrid"));
    }
}
