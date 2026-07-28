using System.Linq;
using DacPac.Core.Generators;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.Core.Tests;

public class TableToCsharpClassGeneratorTest
{
    [Fact]
    public void Build_GeneratesPropertiesWithMappedTypes()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("""
                         CREATE TABLE [dbo].[Orders]
                         (
                             [OrderId] bigint NOT NULL,
                             [CustomerId] int NULL
                         );
                         """);

        var orders = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();
        var output = new TableToCsharpClassGenerator().Build(orders).ToString();

        Assert.Contains("public class Orders", output);
        Assert.Contains("public long OrderId { get; set; }", output);
        Assert.Contains("public int? CustomerId { get; set; }", output);
    }
}
