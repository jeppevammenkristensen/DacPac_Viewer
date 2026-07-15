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
