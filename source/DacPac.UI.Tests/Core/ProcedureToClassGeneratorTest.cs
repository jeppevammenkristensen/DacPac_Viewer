using System;
using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Core;

public class ProcedureToClassGeneratorTest
{
    [Fact]
    public void Build_GeneratesDapperExecuteAndCommentedAlternatives()
    {
        using var model = CreateModel("""
                                  CREATE PROCEDURE [dbo].[GetCustomer]
                                  @Customer_Id int
                                  AS
                                  BEGIN
                                      SELECT @Customer_Id AS CustomerId;
                                  END
        """);

        var procedure = GetProcedure(model);
        var output = new Builder([new ProcedureToClassGenerator()]).Build([procedure]);

        Assert.Contains("public async Task<int> ExecuteAsync(System.Data.IDbConnection connection, Parameters parameters)", output);
        Assert.Contains("public Dapper.DynamicParameters GenerateParameters()", output);
        Assert.Contains("dynamicParameters.Add(\"@Customer_Id\", CustomerId);", output);
        Assert.Contains("var dynamicParameters = parameters.GenerateParameters();", output);
        Assert.Contains("var affectedRows = await Dapper.SqlMapper.ExecuteAsync(", output);
        Assert.Contains("commandType: System.Data.CommandType.StoredProcedure);", output);
        Assert.Contains("// public async Task<System.Collections.Generic.IEnumerable<TResult>> QueryAsync<TResult>", output);
        Assert.Contains("// public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>", output);
        Assert.Contains("// public async Task<Dapper.SqlMapper.GridReader> QueryMultipleAsync", output);
        Assert.Contains("parameters.GenerateParameters()", output);
        Assert.DoesNotContain("public async Task ExecuteAsync", output);
    }

    [Fact]
    public void Build_LeavesParametersOutOfDapperCalls_WhenProcedureHasNoParameters()
    {
        using var model = CreateModel("""
                                  CREATE PROCEDURE [dbo].[Ping]
                                  AS
                                  BEGIN
                                      SELECT 1 AS Result;
                                  END
        """);

        var procedure = GetProcedure(model);
        var output = new Builder([new ProcedureToClassGenerator()]).Build([procedure]);

        Assert.Contains("public async Task<int> ExecuteAsync(System.Data.IDbConnection connection)", output);
        Assert.Contains("// Output parameters are configured in Parameters.GenerateParameters", output);
        Assert.DoesNotContain("Parameters parameters", output);
    }

    [Fact]
    public void Build_MapsOutputParametersThroughDynamicParameters()
    {
        using var model = CreateModel("""
                                  CREATE PROCEDURE [dbo].[SetValue]
                                      @InputValue int,
                                      @OutputValue int OUTPUT
                                  AS
                                  BEGIN
                                      SET @OutputValue = @InputValue;
                                  END
                                  """);

        var procedure = GetProcedure(model);
        var output = new Builder([new ProcedureToClassGenerator()]).Build([procedure]);

        Assert.Contains("dynamicParameters.Add(\"@OutputValue\", OutputValue, dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.InputOutput);", output);
        Assert.Contains("parameters.OutputValue = dynamicParameters.Get<int?>(\"@OutputValue\");", output);
    }

    [Fact]
    public void Build_GeneratesTypedResultForAnAliasedCast()
    {
        using var model = CreateModel("""
                                   CREATE PROCEDURE [dbo].[GetCustomer]
                                   AS
                                   BEGIN
                                       SELECT CAST(1 AS bigint) AS CustomerId, N'Alice' AS Name;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public sealed class Result", output);
        Assert.Contains("public long CustomerId { get; set; }", output);
        Assert.Contains("public string Name { get; set; }", output);
        Assert.Contains("public async Task<System.Collections.Generic.List<Result>> QueryAsync", output);
        Assert.Contains("Dapper.SqlMapper.QueryAsync<Result>", output);
    }

    [Fact]
    public void Build_GeneratesClassesForMultipleResultSets()
    {
        using var model = CreateModel("""
                                   CREATE PROCEDURE [dbo].[GetCustomer]
                                   AS
                                   BEGIN
                                       SELECT 1 AS CustomerId;
                                       SELECT N'Alice' AS Name;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public sealed class Result1", output);
        Assert.Contains("public sealed class Result2", output);
        Assert.Contains("public sealed class Results", output);
        Assert.Contains("multiple or branch-dependent result sets", output);
        Assert.Contains("public async Task<System.Collections.Generic.List<Result2>> QueryAsync", output);
        Assert.Contains("await gridReader.ReadAsync<Result1>();", output);
        Assert.Contains("public async Task<Results> QueryAllAsync", output);
        Assert.Contains("Result1 = new System.Collections.Generic.List<Result1>(await gridReader.ReadAsync<Result1>())", output);
    }

    [Fact]
    public void Build_UsesObjectAndWarningForAnUnknownExpression()
    {
        using var model = CreateModel("""
                                   CREATE PROCEDURE [dbo].[GetCustomer]
                                   AS
                                   BEGIN
                                       SELECT GETDATE() AS CreatedAt;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("The SQL expression 'FunctionCall' could not be statically typed", output);
        Assert.Contains("public object? CreatedAt { get; set; }", output);
    }

    [Fact]
    public void Build_MapsAReferencedTableColumnFromTheDacPacModel()
    {
        using var model = CreateModel("""
                                   CREATE TABLE [dbo].[Customer]
                                   (
                                       [CustomerId] bigint NOT NULL,
                                       [Name] nvarchar(100) NULL
                                   );
                                   GO
                                   CREATE PROCEDURE [dbo].[GetCustomer]
                                   AS
                                   BEGIN
                                       SELECT CustomerId, Name FROM dbo.Customer;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public long CustomerId { get; set; }", output);
        Assert.Contains("public string Name { get; set; }", output);
        Assert.DoesNotContain("could not be resolved unambiguously", output);
    }

    [Fact]
    public void Build_UsesSingularPrimaryTableNameForTheResultClass()
    {
        using var model = CreateModel("""
                                   CREATE TABLE [dbo].[Bars]
                                   (
                                       [Foo] int NOT NULL
                                   );
                                   GO
                                   CREATE PROCEDURE [dbo].[GetBars]
                                   AS
                                   BEGIN
                                       SELECT Foo FROM dbo.Bars;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public sealed class Bar", output);
        Assert.Contains("System.Collections.Generic.List<Bar>> QueryAsync", output);
        Assert.DoesNotContain("public sealed class Result", output);
    }

    [Fact]
    public void Build_UsesSingularPrimaryViewNameForTheResultClass()
    {
        using var model = CreateModel("""
                                   CREATE TABLE [dbo].[SourceBars]
                                   (
                                       [Foo] int NOT NULL
                                   );
                                   GO
                                   CREATE VIEW [dbo].[ActiveBars]
                                   AS
                                   SELECT Foo FROM dbo.SourceBars;
                                   GO
                                   CREATE PROCEDURE [dbo].[GetActiveBars]
                                   AS
                                   BEGIN
                                       SELECT Foo FROM dbo.ActiveBars;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public sealed class ActiveBar", output);
        Assert.Contains("System.Collections.Generic.List<ActiveBar>> QueryAsync", output);
    }

    [Fact]
    public void Build_WarnsWhenConditionalBranchesReturnDifferentShapes()
    {
        using var model = CreateModel("""
                                   CREATE PROCEDURE [dbo].[GetCustomer]
                                       @IncludeName bit
                                   AS
                                   BEGIN
                                       IF @IncludeName = 1
                                           SELECT 1 AS CustomerId, N'Alice' AS Name;
                                       ELSE
                                           SELECT 1 AS CustomerId;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.Contains("public sealed class Result1", output);
        Assert.Contains("public sealed class Result2", output);
        Assert.Contains("multiple or branch-dependent result sets", output);
    }

    [Fact]
    public void Build_DoesNotTreatInsertSelectAsAResultSet()
    {
        using var model = CreateModel("""
                                   CREATE TABLE [dbo].[Customer]
                                   (
                                       [CustomerId] int NOT NULL
                                   );
                                   GO
                                   CREATE PROCEDURE [dbo].[AddCustomer]
                                   AS
                                   BEGIN
                                       INSERT INTO dbo.Customer (CustomerId)
                                       SELECT 1;
                                   END
                                   """);

        var output = new Builder([new ProcedureToClassGenerator()]).Build([GetProcedure(model)]);

        Assert.DoesNotContain("public sealed class Result", output);
        Assert.DoesNotContain("QueryAsync(System.Data.IDbConnection", output);
    }

    private static TSqlModel CreateModel(string procedureScript)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(procedureScript);
        return model;
    }

    private static TSqlObject GetProcedure(TSqlModel model)
    {
        return model.GetObjects(DacQueryScopes.UserDefined, Procedure.TypeClass).Single();
    }
}
