using System.Globalization;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using DacPac.UI.Converters;
using DacPac.UI.Models.LandingPage;
using Xunit;

namespace DacPac.UI.Tests.Converters;

public class TreeItemIconConverterTest
{
    [AvaloniaTheory]
    [InlineData(TreeIconIds.Folder)]
    [InlineData(TreeIconIds.Schema)]
    [InlineData(TreeIconIds.Table)]
    [InlineData(TreeIconIds.Column)]
    [InlineData(TreeIconIds.Parameter)]
    [InlineData(TreeIconIds.View)]
    [InlineData(TreeIconIds.Procedure)]
    public void Convert_ResolvesRegisteredIcon(string iconId)
    {
        var result = new TreeItemIconConverter().Convert(iconId, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.NotNull(result);
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public void Convert_ReturnsNullForUnknownIcon(object? iconId)
    {
        var result = new TreeItemIconConverter().Convert(iconId, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Null(result);
    }

    [AvaloniaFact]
    public void ConvertBack_ReturnsDoNothing()
    {
        var result = new TreeItemIconConverter().ConvertBack(null, typeof(object), null, CultureInfo.InvariantCulture);

        Assert.Same(BindingOperations.DoNothing, result);
    }
}