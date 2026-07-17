using System.Text;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

/// <summary>
/// Generates a Dapper-backed C# API and result models for a DacPac stored procedure.
/// </summary>
public class ProcedureToClassGenerator : CsharpGenerator
{
    private readonly ProcedureResultSetAnalyzer _resultSetAnalyzer = new();

    /// <summary>
    /// Writes the procedure wrapper, inferred result types, query methods, and parameter model.
    /// </summary>
    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        sb.AppendLine($"""
                       /// <summary>
                       /// Represents a {sqlObject.Name.Parts.Last()} {sqlObject.Name.ToString()}
                       /// </summary>
                       """);
        sb.AppendLine($"public class {sqlObject.Name.Parts.Last().ToPascalCase()}Procedure");
        sb.AppendLine("{");

        // Infer typed result DTOs before emitting the Dapper methods that consume them.
        var resultSets = _resultSetAnalyzer.Analyze(sqlObject);
        BuildResultClasses(resultSets, sb);

        // Parameters are emitted after methods so the generated API reads from operations to configuration.
        var parameters = new StringBuilder();
        var (_, hasParameters) = BuildParametersObject(sqlObject, parameters);

        var procedureName = EscapeCSharpStringLiteral(sqlObject.Name.ToString());
        var parameterDeclaration = hasParameters ? ", Parameters parameters" : string.Empty;

        sb.AppendLine("// Requires the Dapper NuGet package.");
        sb.AppendLine($"private const string ProcedureName = \"{procedureName}\";");
        sb.AppendLine();
        sb.AppendLine($"public async Task<int> ExecuteAsync(System.Data.IDbConnection connection{parameterDeclaration})");
        sb.AppendLine("{");
        if (hasParameters)
        {
            sb.AppendLine("var dynamicParameters = parameters.GenerateParameters();");
        }

        sb.AppendLine("var affectedRows = await Dapper.SqlMapper.ExecuteAsync(");
        sb.AppendLine("connection,");
        sb.AppendLine("ProcedureName,");
        sb.AppendLine($"{(hasParameters ? "dynamicParameters" : "null")},");
        sb.AppendLine("commandType: System.Data.CommandType.StoredProcedure);");

        foreach (var parameter in sqlObject.GetReferenced(Procedure.Parameters).Where(x => x.GetProperty<bool>(Parameter.IsOutput)))
        {
            var parameterName = parameter.Name.Parts.Last();
            var propertyName = parameterName.TrimStart('@').ToPascalCase();
            var dataType = parameter.GetReferenced(Parameter.DataType).FirstOrDefault();
            var dotnetType = dataType?.GetDotNetDataType(parameter.GetProperty<bool>(Parameter.IsNullable));

            sb.AppendLine($"parameters.{propertyName} = dynamicParameters.Get<{dotnetType?.ToString() ?? "object"}>(\"{EscapeCSharpStringLiteral(parameterName)}\");");
        }

        sb.AppendLine("return affectedRows;");
        sb.AppendLine("}");

        // Generate query methods only when the procedure exposes one or more result sets.
        BuildDapperAlternatives(sb, hasParameters, resultSets);

        sb.AppendLine(parameters.ToString());

        sb.AppendLine("}");
    }

    /// <summary>
    /// Maps a DacFx data type to the Dapper <see cref="System.Data.DbType"/> name required for output parameters.
    /// </summary>
    private static string? GetDbType(TSqlObject? dataType)
    {
        if (dataType == null)
        {
            return null;
        }

        if (dataType.GetReferenced(DataType.Type).FirstOrDefault() is { } underlying)
        {
            return GetDbType(underlying);
        }

        return dataType.GetProperty<SqlDataType>(DataType.SqlDataType) switch
        {
            SqlDataType.BigInt => "Int64",
            SqlDataType.Binary or SqlDataType.Image or SqlDataType.Timestamp or SqlDataType.Rowversion or SqlDataType.VarBinary => "Binary",
            SqlDataType.Bit => "Boolean",
            SqlDataType.Char => "AnsiStringFixedLength",
            SqlDataType.Date => "Date",
            SqlDataType.DateTime or SqlDataType.SmallDateTime => "DateTime",
            SqlDataType.DateTime2 => "DateTime2",
            SqlDataType.DateTimeOffset => "DateTimeOffset",
            SqlDataType.Decimal or SqlDataType.Money or SqlDataType.Numeric or SqlDataType.SmallMoney => "Decimal",
            SqlDataType.Float => "Double",
            SqlDataType.Int => "Int32",
            SqlDataType.NChar => "StringFixedLength",
            SqlDataType.NText or SqlDataType.NVarChar or SqlDataType.Xml or SqlDataType.Json => "String",
            SqlDataType.Real => "Single",
            SqlDataType.SmallInt => "Int16",
            SqlDataType.Text or SqlDataType.VarChar => "AnsiString",
            SqlDataType.Time => "Time",
            SqlDataType.TinyInt => "Byte",
            SqlDataType.UniqueIdentifier => "Guid",
            _ => null
        };
    }

    /// <summary>
    /// Writes one generated result class for each statically detected procedure result set.
    /// </summary>
    private static void BuildResultClasses(IReadOnlyList<ProcedureResultSet> resultSets, StringBuilder sb)
    {
        foreach (var resultSet in resultSets)
        {
            var className = GetResultClassName(resultSet, resultSets);
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Represents result set {resultSet.Ordinal} returned by the procedure.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public sealed class {className}");
            sb.AppendLine("{");
            foreach (var warning in resultSet.Warnings)
            {
                sb.AppendLine($"// Warning: {warning}");
            }

            foreach (var column in resultSet.Columns)
            {
                sb.AppendLine($"/// <summary>Gets or sets the {column.PropertyName} value returned by the procedure.</summary>");
                sb.AppendLine($"public {column.Type} {column.PropertyName} {{ get; set; }}");
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }
    }

    /// <summary>
    /// Writes the aggregate type returned by the generated multi-result query method.
    /// </summary>
    private static void BuildAllResultsClass(IReadOnlyList<ProcedureResultSet> resultSets, StringBuilder sb)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Contains every result set returned by the procedure.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class Results");
        sb.AppendLine("{");
        foreach (var resultSet in resultSets)
        {
            var resultType = GetResultClassName(resultSet, resultSets);
            sb.AppendLine($"/// <summary>Gets the rows from result set {resultSet.Ordinal}.</summary>");
            sb.AppendLine($"public System.Collections.Generic.List<{resultType}> {resultType} {{ get; init; }} = [];");
        }

        sb.AppendLine("}");
        sb.AppendLine();
    }

    /// <summary>
    /// Writes Dapper query methods suited to the number of procedure result sets and generic usage alternatives.
    /// </summary>
    private static void BuildDapperAlternatives(StringBuilder sb, bool hasParameters, IReadOnlyList<ProcedureResultSet> resultSets)
    {
        var parameterDeclaration = hasParameters ? ", Parameters parameters" : string.Empty;
        var parameterArgument = hasParameters ? "parameters.GenerateParameters()" : "null";

        if (resultSets.Count == 1)
        {
            var resultType = GetResultClassName(resultSets[0], resultSets);
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Executes the procedure and returns its rows.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public async Task<System.Collections.Generic.List<{resultType}>> QueryAsync(System.Data.IDbConnection connection{parameterDeclaration})");
            sb.AppendLine("{");
            sb.AppendLine($"return new System.Collections.Generic.List<{resultType}>(await Dapper.SqlMapper.QueryAsync<{resultType}>(connection, ProcedureName, {parameterArgument}, commandType: System.Data.CommandType.StoredProcedure));");
            sb.AppendLine("}");
        }
        else if (resultSets.Count > 1)
        {
            BuildAllResultsClass(resultSets, sb);
            sb.AppendLine();
            sb.AppendLine("// Warning: this procedure has multiple or branch-dependent result sets. Use QueryMultipleAsync and read each result type explicitly.");
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Executes the procedure and returns the rows from its final result set.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public async Task<System.Collections.Generic.List<{GetResultClassName(resultSets[^1], resultSets)}>> QueryAsync(System.Data.IDbConnection connection{parameterDeclaration})");
            sb.AppendLine("{");
            sb.AppendLine($"using var gridReader = await Dapper.SqlMapper.QueryMultipleAsync(connection, ProcedureName, {parameterArgument}, commandType: System.Data.CommandType.StoredProcedure);");
            foreach (var resultSet in resultSets.Take(resultSets.Count - 1))
            {
                sb.AppendLine($"await gridReader.ReadAsync<{GetResultClassName(resultSet, resultSets)}>();");
            }
            sb.AppendLine($"return new System.Collections.Generic.List<{GetResultClassName(resultSets[^1], resultSets)}>(await gridReader.ReadAsync<{GetResultClassName(resultSets[^1], resultSets)}>());");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("/// <summary>");
            sb.AppendLine("/// Executes the procedure and returns all of its result sets.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"public async Task<Results> QueryAllAsync(System.Data.IDbConnection connection{parameterDeclaration})");
            sb.AppendLine("{");
            sb.AppendLine($"using var gridReader = await Dapper.SqlMapper.QueryMultipleAsync(connection, ProcedureName, {parameterArgument}, commandType: System.Data.CommandType.StoredProcedure);");
            sb.AppendLine("return new Results");
            sb.AppendLine("{");
            foreach (var resultSet in resultSets)
            {
                var resultType = GetResultClassName(resultSet, resultSets);
                sb.AppendLine($"{resultType} = new System.Collections.Generic.List<{resultType}>(await gridReader.ReadAsync<{resultType}>()),");
            }
            sb.AppendLine("};");
            sb.AppendLine("}");
        }

        sb.AppendLine();
        sb.AppendLine("// Alternative: query multiple rows from the procedure.");
        sb.AppendLine($"// public async Task<System.Collections.Generic.IEnumerable<TResult>> QueryAsync<TResult>(System.Data.IDbConnection connection{parameterDeclaration})");
        sb.AppendLine("// {");
        sb.AppendLine("//     return await Dapper.SqlMapper.QueryAsync<TResult>(");
        sb.AppendLine("//         connection,");
        sb.AppendLine("//         ProcedureName,");
        sb.AppendLine($"//         {parameterArgument},");
        sb.AppendLine("//         commandType: System.Data.CommandType.StoredProcedure);");
        sb.AppendLine("// }");
        sb.AppendLine();
        sb.AppendLine("// Alternative: query one row, or null when no row is returned.");
        sb.AppendLine($"// public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(System.Data.IDbConnection connection{parameterDeclaration})");
        sb.AppendLine("// {");
        sb.AppendLine("//     return await Dapper.SqlMapper.QuerySingleOrDefaultAsync<TResult>(");
        sb.AppendLine("//         connection,");
        sb.AppendLine("//         ProcedureName,");
        sb.AppendLine($"//         {parameterArgument},");
        sb.AppendLine("//         commandType: System.Data.CommandType.StoredProcedure);");
        sb.AppendLine("// }");
        sb.AppendLine();
        sb.AppendLine("// Alternative: consume multiple result sets.");
        sb.AppendLine($"// public async Task<Dapper.SqlMapper.GridReader> QueryMultipleAsync(System.Data.IDbConnection connection{parameterDeclaration})");
        sb.AppendLine("// {");
        sb.AppendLine("//     return await Dapper.SqlMapper.QueryMultipleAsync(");
        sb.AppendLine("//         connection,");
        sb.AppendLine("//         ProcedureName,");
        sb.AppendLine($"//         {parameterArgument},");
        sb.AppendLine("//         commandType: System.Data.CommandType.StoredProcedure);");
        sb.AppendLine("// }");
        sb.AppendLine();
        sb.AppendLine("// Output parameters are configured in Parameters.GenerateParameters and copied back by ExecuteAsync.");
    }

    /// <summary>
    /// Selects the generated result class name for a procedure with one or multiple result sets.
    /// </summary>
    private static string GetResultClassName(ProcedureResultSet resultSet, IReadOnlyList<ProcedureResultSet> resultSets)
    {
        if (resultSet.SourceName is null)
        {
            return resultSets.Count == 1 ? "Result" : $"Result{resultSet.Ordinal}";
        }

        var matchingSources = resultSets
            .Where(x => string.Equals(x.SourceName, resultSet.SourceName, StringComparison.Ordinal))
            .ToArray();
        return matchingSources.Length == 1
            ? resultSet.SourceName
            : $"{resultSet.SourceName}{Array.IndexOf(matchingSources, resultSet) + 1}";
    }

    /// <summary>
    /// Escapes a value for safe inclusion in a generated C# string literal.
    /// </summary>
    private static string EscapeCSharpStringLiteral(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    /// <summary>
    /// Writes the nested parameter model and its Dapper parameter conversion method when parameters exist.
    /// </summary>
    private (StringBuilder, bool) BuildParametersObject(TSqlObject sqlObject, StringBuilder sb)
    {
        var parameters = sqlObject.GetReferenced(Procedure.Parameters).ToList();
        if (parameters.Any())
        {
            sb.AppendLine($"""
                           /// <summary>
                           /// Represents the parameters for the {sqlObject.Name.Parts.Last()} procedure.
                           /// </summary>
                           """);
            sb.AppendLine($"public class Parameters");
            sb.AppendLine("{");

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Name.Parts.Last();
                var dataType = parameter.GetReferenced(Parameter.DataType).FirstOrDefault();
                var isNullable = parameter.GetProperty<bool>(Parameter.IsNullable);

                sb.AppendLine($"""
                               /// <summary>
                               /// Gets or sets the {parameterName} ({dataType?.Name.ToString()}){(isNullable ? " (nullable)" : "")}.
                               /// </summary>
                               """);

                var dotnetType = dataType == null ? null : ExtensionMethods.GetDotNetDataType(dataType, isNullable);
                if (dotnetType == null)
                {
                    sb.AppendLine($"// Warning: Unrecognized SQL data type '{dataType}' for parameter '{parameterName}'.");
                    sb.AppendLine($"public object {parameterName.TrimStart('@').ToPascalCase()} {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine($"public {dotnetType} {parameterName.TrimStart('@').ToPascalCase()} {{ get; set; }}");
                }
            }

            sb.AppendLine();
            sb.AppendLine("public Dapper.DynamicParameters GenerateParameters()");
            sb.AppendLine("{");
            sb.AppendLine("var dynamicParameters = new Dapper.DynamicParameters();");

            foreach (var parameter in parameters)
            {
                var parameterName = parameter.Name.Parts.Last();
                var propertyName = parameterName.TrimStart('@').ToPascalCase();
                var dataType = parameter.GetReferenced(Parameter.DataType).FirstOrDefault();
                var dbType = parameter.GetProperty<bool>(Parameter.IsOutput) ? GetDbType(dataType) : null;
                var outputArguments = parameter.GetProperty<bool>(Parameter.IsOutput)
                    ? $", {(dbType == null ? string.Empty : $"dbType: System.Data.DbType.{dbType}, ")}direction: System.Data.ParameterDirection.InputOutput"
                    : string.Empty;

                sb.AppendLine($"dynamicParameters.Add(\"{EscapeCSharpStringLiteral(parameterName)}\", {propertyName}{outputArguments});");
            }

            sb.AppendLine("return dynamicParameters;");
            sb.AppendLine("}");
            sb.AppendLine("}");
            return (sb, true);
        }

        return (sb, false);
    }


    /// <summary>
    /// Determines whether the object is a named stored procedure.
    /// </summary>
    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == Procedure.TypeClass && tSqlObject.Name.HasName;
    }
}
