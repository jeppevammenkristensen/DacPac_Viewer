using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DacPac.Core;

/// <summary>
/// Maps the output columns of a DacPac view to the expressions in its SELECT statement.
/// </summary>
public sealed class ViewSelectStatementAnalyzer
{
    /// <summary>
    /// Analyzes the view's SELECT statement and returns its output columns in declaration order.
    /// </summary>
    /// <param name="view">The DacPac view to analyze.</param>
    /// <returns>The view columns and their corresponding SELECT expressions.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="view"/> is not a view.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the SELECT statement cannot be parsed.</exception>
    /// <exception cref="NotSupportedException">Thrown when the SELECT shape cannot be mapped to the view columns.</exception>
    public IReadOnlyList<ViewSelectColumn> Analyze(TSqlObject view)
    {
        ArgumentNullException.ThrowIfNull(view);
        view.ThrowIfIncorrectType(View.TypeClass);

        var sql = view.GetProperty(View.SelectStatement)?.ToString();
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException($"View '{view.Name}' does not contain a SELECT statement.");
        }

        var parser = new TSql170Parser(initialQuotedIdentifiers: true);
        var fragment = parser.Parse(new StringReader(sql), out var errors);
        if (errors.Count > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                errors.Select(error => $"Line {error.Line}, column {error.Column}: {error.Message}"));
            throw new InvalidOperationException($"Could not parse the SELECT statement for view '{view.Name}':{Environment.NewLine}{details}");
        }

        var selectStatement = ((TSqlScript)fragment).Batches
            .SelectMany(batch => batch.Statements)
            .OfType<SelectStatement>()
            .SingleOrDefault()
            ?? throw new NotSupportedException($"View '{view.Name}' must contain one SELECT statement.");

        if (selectStatement.QueryExpression is not QuerySpecification query)
        {
            throw new NotSupportedException($"View '{view.Name}' does not use a directly mappable SELECT query.");
        }

        var outputColumns = view.GetReferenced(View.Columns).ToArray();
        var selectExpressions = query.SelectElements.OfType<SelectScalarExpression>().ToArray();
        if (selectExpressions.Length != query.SelectElements.Count || outputColumns.Length != selectExpressions.Length)
        {
            throw new NotSupportedException(
                $"View '{view.Name}' has {outputColumns.Length} output columns, but its SELECT statement has " +
                $"{selectExpressions.Length} directly mappable scalar expressions.");
        }

        var sourceColumns = view.GetReferenced(View.BodyDependencies)
            .Where(source => source.IsAnyOfType(Table.TypeClass, View.TypeClass))
            .SelectMany(GetColumns)
            .GroupBy(column => column.Name.Parts.Last(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        return outputColumns
            .Zip(selectExpressions, (column, selected) => Analyze(column, selected, sql, sourceColumns))
            .ToArray();
    }

    /// <summary>
    /// Maps one view output column to its scalar SELECT expression.
    /// </summary>
    private static ViewSelectColumn Analyze(
        TSqlObject column,
        SelectScalarExpression selected,
        string sql,
        IReadOnlyDictionary<string, TSqlObject[]> sourceColumns)
    {
        var function = selected.Expression as FunctionCall;
        var functionCall = function is null
            ? null
            : new ViewFunctionCall(
                function.FunctionName.Value,
                function.Parameters.Select(parameter => GetText(parameter, sql)).ToArray(),
                function.Parameters.Select(parameter => GetType(parameter, sourceColumns)).ToArray());

        return new ViewSelectColumn(column, GetText(selected.Expression, sql), functionCall);
    }

    /// <summary>
    /// Gets the columns exposed by a table or view referenced in the view body.
    /// </summary>
    private static IEnumerable<TSqlObject> GetColumns(TSqlObject source)
    {
        return source.ObjectType == Table.TypeClass
            ? source.GetReferenced(Table.Columns)
            : source.GetReferenced(View.Columns);
    }

    /// <summary>
    /// Resolves a function argument type from a literal or an unambiguous referenced source column.
    /// </summary>
    private static DotnetType? GetType(
        ScalarExpression expression,
        IReadOnlyDictionary<string, TSqlObject[]> sourceColumns)
    {
        if (expression is ColumnReferenceExpression reference)
        {
            var name = reference.MultiPartIdentifier.Identifiers.LastOrDefault()?.Value;
            if (name is not null && sourceColumns.TryGetValue(name, out var matches) && matches.Length == 1)
            {
                var sourceColumn = matches[0];
                var dataType = sourceColumn.GetReferenced(Column.DataType).FirstOrDefault();
                return dataType?.GetDotNetDataType(sourceColumn.GetProperty<bool>(Column.Nullable));
            }
        }

        return expression switch
        {
            IntegerLiteral => new DotnetType("int", false),
            NumericLiteral or MoneyLiteral => new DotnetType("decimal", false),
            StringLiteral => new DotnetType("string", false),
            _ => null
        };
    }

    /// <summary>
    /// Gets the original SQL represented by a parsed ScriptDom fragment.
    /// </summary>
    private static string GetText(TSqlFragment fragment, string sql)
    {
        return sql.Substring(fragment.StartOffset, fragment.FragmentLength);
    }
}

/// <summary>
/// Describes one output column and its expression in a view SELECT statement.
/// </summary>
/// <param name="ColumnObject">The DacPac output column.</param>
/// <param name="Expression">The original SQL expression that produces the column.</param>
/// <param name="Function">The top-level function call, or <see langword="null"/> for another expression type.</param>
public sealed record ViewSelectColumn(
    TSqlObject ColumnObject,
    string Expression,
    ViewFunctionCall? Function)
{
    private static readonly HashSet<string> KnownStringFunctions = new(
        [
            "Char", "Concat", "Concat_Ws", "Datename", "Format", "Json_Query", "Json_Value", "Left", "Lower",
            "LTrim", "NChar", "Quotename", "Replace", "Replicate", "Reverse", "Right", "RTrim", "Soundex",
            "Space", "Str", "String_Agg", "String_Escape", "Stuff", "Substring", "Translate", "Trim", "Upper"
        ],
        StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> KnownNumericFunctionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Abs"] = "decimal",
        ["Acos"] = "double",
        ["Approx_Count_Distinct"] = "long",
        ["Ascii"] = "int",
        ["Asin"] = "double",
        ["Atan"] = "double",
        ["Atn2"] = "double",
        ["Avg"] = "decimal",
        ["Binary_Checksum"] = "int",
        ["Ceiling"] = "decimal",
        ["Charindex"] = "int",
        ["Checksum"] = "int",
        ["Checksum_Agg"] = "int",
        ["Cos"] = "double",
        ["Cot"] = "double",
        ["Count"] = "int",
        ["Count_Big"] = "long",
        ["Datediff"] = "int",
        ["Datediff_Big"] = "long",
        ["Degrees"] = "decimal",
        ["Dense_Rank"] = "long",
        ["Difference"] = "int",
        ["Exp"] = "double",
        ["Floor"] = "decimal",
        ["Grouping"] = "int",
        ["Grouping_Id"] = "int",
        ["Isnumeric"] = "int",
        ["Len"] = "int",
        ["Log"] = "double",
        ["Log10"] = "double",
        ["Ntile"] = "long",
        ["Patindex"] = "int",
        ["Percent_Rank"] = "double",
        ["Pi"] = "double",
        ["Power"] = "decimal",
        ["Radians"] = "decimal",
        ["Rand"] = "double",
        ["Rank"] = "long",
        ["Round"] = "decimal",
        ["Row_Number"] = "long",
        ["Sign"] = "decimal",
        ["Sin"] = "double",
        ["Sqrt"] = "double",
        ["Square"] = "decimal",
        ["Stdev"] = "double",
        ["Stdevp"] = "double",
        ["Sum"] = "decimal",
        ["Tan"] = "double",
        ["Unicode"] = "int",
        ["Var"] = "double",
        ["Varp"] = "double"
    };

    private static readonly HashSet<string> PromotedNumericAggregateFunctions = new(
        ["Avg", "Sum"],
        StringComparer.OrdinalIgnoreCase);
    
    /// <summary>
    /// Resolves the column's CLR type from DacFx metadata, referenced columns, or a known SQL function.
    /// </summary>
    /// <returns>The resolved type and whether it was inferred from the function name.</returns>
    public (DotnetType? property, bool guessed) GuessType()
    {
        var nullable = ColumnObject.GetProperty<bool>(Column.Nullable);
        
        if (ColumnObject.GetReferenced(Column.DataType).ToList() is {Count: 1} dataType)
        {
            return (dataType[0].GetDotNetDataType(nullable), false);
        }
        else if (ColumnObject.GetReferencedRelationshipInstances(DacExternalQueryScopes.All).ToList() is
                 {Count: > 0} referenced &&
                 referenced[0].Object.GetReferenced(Column.DataType).FirstOrDefault() is {} objectType)
        {
            var referencedType = objectType.GetDotNetDataType(nullable);
            if (referencedType is not null && Function is { } function &&
                PromotedNumericAggregateFunctions.Contains(function.Name))
            {
                return (PromoteNumericAggregateType(referencedType, nullable), true);
            }

            return (referencedType, false);
        }
        else if (Function is {} function)
        {
            if (PromotedNumericAggregateFunctions.Contains(function.Name) &&
                function.ArgumentTypes.FirstOrDefault() is { } argumentType)
            {
                return (PromoteNumericAggregateType(argumentType, nullable), true);
            }

            if (KnownNumericFunctionTypes.TryGetValue(function.Name, out var numericType))
            {
                return (new DotnetType(numericType, nullable), true);
            }
            else if (KnownStringFunctions.Contains(function.Name))
            {
                return (new DotnetType("string", nullable), true);
            }

            return (null, true);

        }

        return (null, false);

    }

    /// <summary>
    /// Applies SQL Server's documented return-type promotion for <c>AVG</c> and <c>SUM</c>.
    /// </summary>
    private static DotnetType PromoteNumericAggregateType(DotnetType inputType, bool nullable)
    {
        var typeName = inputType.Name switch
        {
            "byte" or "short" or "int" => "int",
            "long" => "long",
            "decimal" => "decimal",
            "float" or "double" => "double",
            _ => inputType.Name
        };

        return new DotnetType(typeName, nullable);
    }
}

/// <summary>
/// Describes a top-level function call used as a view column expression.
/// </summary>
/// <param name="Name">The SQL function name.</param>
/// <param name="Arguments">The function arguments as written in the SELECT statement.</param>
/// <param name="ArgumentTypes">The statically resolved CLR type for each argument, when available.</param>
public sealed record ViewFunctionCall(
    string Name,
    IReadOnlyList<string> Arguments,
    IReadOnlyList<DotnetType?> ArgumentTypes);
