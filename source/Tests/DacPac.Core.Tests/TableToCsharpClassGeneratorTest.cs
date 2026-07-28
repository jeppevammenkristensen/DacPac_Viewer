using System.Linq;
using DacPac.Core.Generators;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.Core.Tests;

public class TableToCsharpClassGeneratorTest
{
    [Fact]
    public void Build_GeneratesPropertiesAndForeignKeyRemarks()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("""
                         CREATE TABLE [dbo].[Customer]
                         (
                             [CustomerId] int NOT NULL
                         );
                         GO
                         CREATE TABLE [dbo].[Orders]
                         (
                             [OrderId] bigint NOT NULL,
                             [CustomerId] int NULL,
                             CONSTRAINT [FK_Orders_Customer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([CustomerId])
                         );
                         """);

        var tables = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).ToList();
        var orders = tables.Single(x => x.Name.Parts.Last() == "Orders");
        var customer = tables.Single(x => x.Name.Parts.Last() == "Customer");
        var output = new TableToCsharpClassGenerator().Build(orders).ToString();
        var customerOutput = new TableToCsharpClassGenerator().Build(customer).ToString();

        Assert.Contains("public class Orders", output);
        Assert.Contains("public long OrderId { get; set; }", output);
        Assert.Contains("public int? CustomerId { get; set; }", output);
        Assert.Contains("Foreign key pointing to [dbo].[Customer].[CustomerId] in [dbo].[Customer]", output);
        Assert.Contains("Key referenced by [dbo].[Orders].[CustomerId] in [dbo].[Orders]", customerOutput);
    }
}
