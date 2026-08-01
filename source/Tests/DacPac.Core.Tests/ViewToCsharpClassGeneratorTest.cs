using System.Linq;
using DacPac.Core.Generators;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.Core.Tests;

public class ViewToCsharpClassGeneratorTest
{
    [Fact]
    public void Build_IncludesTheViewScriptInRemarks()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("""
                         CREATE VIEW [dbo].[CustomerCounts]
                         AS
                         SELECT 1 AS [Value & Count];
                         """);

        var view = model.GetObjects(DacQueryScopes.UserDefined, View.TypeClass).Single();
        var output = new Builder([new ViewToCsharpClassGenerator()], NullLogger<Builder>.Instance).Build([view]);

        Assert.Contains("/// <remarks>", output);
        Assert.Contains("/// <code>", output);
        Assert.Contains("CREATE VIEW", output);
        Assert.Contains("/// SELECT 1 AS [Value &amp; Count];", output);
        Assert.Contains("/// </code>", output);
        Assert.Contains("/// </remarks>", output);
    }
}
