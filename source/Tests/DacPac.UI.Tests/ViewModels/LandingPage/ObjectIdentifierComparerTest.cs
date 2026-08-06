using DacPac.UI.ViewModels.LandingPage;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.ViewModels.LandingPage;

public class ObjectIdentifierComparerTest
{
    [Fact]
    public void Equals_UsesNameAndExternalParts()
    {
        var comparer = new ObjectIdentifierComparer();
        var first = new ObjectIdentifier("dbo", "Customer");
        var second = new ObjectIdentifier("dbo", "Customer");
        var different = new ObjectIdentifier("dbo", "Order");

        Assert.True(comparer.Equals(first, second));
        Assert.Equal(comparer.GetHashCode(first), comparer.GetHashCode(second));
        Assert.False(comparer.Equals(first, different));
        Assert.False(comparer.Equals(first, null));
        Assert.True(comparer.Equals(null, null));
    }
}