using System.Linq;
using DacPac.Core.Generators;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.Core.Tests;

public class TableTypeToClassGeneratorTest
{
    [Fact]
    public void Build_GeneratesDataTableConversionMethod()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("""
                         CREATE TYPE [dbo].[OrderItemType] AS TABLE
                         (
                             [ProductId] int NOT NULL,
                             [Description] nvarchar(100) NULL
                         );
                         """);

        var tableType = model.GetObjects(DacQueryScopes.UserDefined, TableType.TypeClass).Single();
        var output = new TableTypeToClassGenerator().Build(tableType).ToString();

        Assert.Contains("public static System.Data.DataTable ToDataTable(System.Collections.Generic.IEnumerable<OrderItemType_TableType> rows)", output);
        Assert.Contains("table.Columns.Add(\"ProductId\", typeof(int));", output);
        Assert.Contains("table.Columns.Add(\"Description\", typeof(string));", output);
        Assert.Contains("(object?)item.ProductId ?? System.DBNull.Value", output);
        Assert.Contains("(object?)item.ProductId ?? System.DBNull.Value,", output);
        Assert.DoesNotContain("(object?)item.Description ?? System.DBNull.Value,", output);
    }
}
