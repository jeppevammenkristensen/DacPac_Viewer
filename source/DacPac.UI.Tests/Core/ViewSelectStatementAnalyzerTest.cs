using System;
using System.Linq;
using DacPac.Core;
using DacPac.UI.ViewModels.Displays;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Core;

public class ViewSelectStatementAnalyzerTest
{
    private readonly ViewSelectStatementAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_MapsOutputColumnsToExpressionsAndFunctions()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Customers]
                                      (
                                          [CustomerId] int NOT NULL,
                                          [Name] nvarchar(100) NULL
                                      );
                                      GO
                                      CREATE VIEW [dbo].[CustomerNames]
                                      AS
                                      SELECT CustomerId, UPPER(Name) AS DisplayName
                                      FROM dbo.Customers;
                                      """);

        var columns = _analyzer.Analyze(GetView(model));

        Assert.Collection(
            columns,
            column =>
            {
                Assert.Equal("CustomerId", column.ColumnObject.Name.Parts.Last());
                Assert.Equal("CustomerId", column.Expression);
                Assert.Null(column.Function);
            },
            column =>
            {
                Assert.Equal("DisplayName", column.ColumnObject.Name.Parts.Last());
                Assert.Equal("UPPER(Name)", column.Expression);
                Assert.Equal("UPPER", column.Function?.Name);
                Assert.Equal(["Name"], column.Function?.Arguments);
            });
    }

    [Fact]
    public void GuessType_RecognizesCommonStringAndNumericFunctions()
    {
        using var model = CreateModel("""
                                      CREATE VIEW [dbo].[CalculatedValues]
                                      AS
                                      SELECT
                                          CONCAT('Hello', ' ', 'World') AS TextValue,
                                          ASCII('A') AS IntegerValue,
                                          SQRT(4) AS FloatingPointValue,
                                          ROUND(12.34, 1) AS DecimalValue;
                                      """);

        var columns = _analyzer.Analyze(GetView(model));

        Assert.Equal("string", columns[0].GuessType().property?.Name);
        Assert.Equal("int", columns[1].GuessType().property?.Name);
        Assert.Equal("double", columns[2].GuessType().property?.Name);
        Assert.Equal("decimal", columns[3].GuessType().property?.Name);
    }

    [Fact]
    public void GuessType_UsesArgumentTypeForArgumentDependentNumericFunctions()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Metrics]
                                      (
                                          [BigValue] bigint NOT NULL,
                                          [DecimalValue] decimal(10, 2) NOT NULL
                                      );
                                      GO
                                      CREATE VIEW [dbo].[CalculatedMetrics]
                                      AS
                                      SELECT
                                          ABS(1) AS AbsoluteInteger,
                                          ABS(BigValue) AS AbsoluteBigValue,
                                          ROUND(DecimalValue, 2) AS RoundedDecimalValue
                                      FROM dbo.Metrics;
                                      """);

        var columns = _analyzer.Analyze(GetView(model));

        Assert.Equal("int", columns[0].GuessType().property?.Name);
        Assert.Equal("long", columns[1].GuessType().property?.Name);
        Assert.Equal("decimal", columns[2].GuessType().property?.Name);
    }

    [Fact]
    public void GuessType_AppliesAggregateReturnTypePromotion()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Metrics]
                                      (
                                          [SmallValue] smallint NOT NULL,
                                          [BigValue] bigint NOT NULL,
                                          [RealValue] real NOT NULL,
                                          [DecimalValue] decimal(10, 2) NOT NULL
                                      );
                                      GO
                                      CREATE VIEW [dbo].[MetricTotals]
                                      AS
                                      SELECT
                                          AVG(SmallValue) AS AverageSmallValue,
                                          SUM(BigValue) AS TotalBigValue,
                                          AVG(RealValue) AS AverageRealValue,
                                          SUM(DecimalValue) AS TotalDecimalValue
                                      FROM dbo.Metrics;
                                      """);

        var columns = _analyzer.Analyze(GetView(model));

        Assert.Equal("int", columns[0].GuessType().property?.Name);
        Assert.Equal("long", columns[1].GuessType().property?.Name);
        Assert.Equal("double", columns[2].GuessType().property?.Name);
        Assert.Equal("decimal", columns[3].GuessType().property?.Name);
    }

    [Fact]
    public void Analyze_RejectsObjectsThatAreNotViews()
    {
        using var model = CreateModel("CREATE TABLE [dbo].[Customers] ([CustomerId] int NOT NULL);");
        var table = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();

        var exception = Assert.Throws<ArgumentException>(() => _analyzer.Analyze(table));

        Assert.Equal("view", exception.ParamName);
    }

    [Fact]
    public void Analyze_ReportsSelectShapesThatCannotBeMapped()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Customers]
                                      (
                                          [CustomerId] int NOT NULL,
                                          [Name] nvarchar(100) NULL
                                      );
                                      GO
                                      CREATE VIEW [dbo].[CustomerNames]
                                      AS
                                      SELECT * FROM dbo.Customers;
                                      """);

        var exception = Assert.Throws<NotSupportedException>(() => _analyzer.Analyze(GetView(model)));

        Assert.Contains("2 output columns", exception.Message);
        Assert.Contains("0 directly mappable scalar expressions", exception.Message);
    }

    [Fact]
    public void ViewDisplayViewModel_FallsBackToDacpacColumnsWhenExpressionsCannotBeMapped()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Customers]
                                      (
                                          [CustomerId] int NOT NULL,
                                          [Name] nvarchar(100) NULL
                                      );
                                      GO
                                      CREATE VIEW [dbo].[CustomerNames]
                                      AS
                                      SELECT * FROM dbo.Customers;
                                      """);

        var viewModel = new ViewDisplayViewModel(GetView(model));

        Assert.Collection(
            viewModel.ColumnWrappers,
            column =>
            {
                Assert.Equal("CustomerId", column.ColumnName);
                Assert.Equal("Expression unavailable", column.Expression);
            },
            column =>
            {
                Assert.Equal("Name", column.ColumnName);
                Assert.Equal("Expression unavailable", column.Expression);
            });
    }

    private static TSqlModel CreateModel(string script)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(script);
        return model;
    }

    private static TSqlObject GetView(TSqlModel model)
    {
        return model.GetObjects(DacQueryScopes.UserDefined, View.TypeClass).Single();
    }
}
