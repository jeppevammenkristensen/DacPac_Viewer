using Humanizer;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DacPac.Core;

/// <summary>
/// Analyses a stored procedure script and describes the row sets returned by its top-level SELECT statements.
/// </summary>
/// <remarks>
/// Types are inferred only when they can be proven from SQL syntax or DacPac metadata. Expressions that cannot be
/// resolved are represented as <c>object?</c> and include a warning in the corresponding result set.
/// </remarks>
public sealed class ProcedureResultSetAnalyzer
{
    /// <summary>
    /// Gets the statically detectable result sets returned by the supplied stored procedure.
    /// </summary>
    /// <remarks>
    /// Result sets are returned in script order. Conditional branches can therefore produce multiple descriptions even
    /// when only one branch is executed at runtime.
    /// </remarks>
    /// <param name="procedure">The DacPac stored procedure to analyse.</param>
    /// <returns>The inferred result sets and any warnings produced while mapping their columns.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="procedure"/> is not a stored procedure.</exception>
    public IReadOnlyList<ProcedureResultSet> Analyze(TSqlObject procedure)
    {
        ArgumentNullException.ThrowIfNull(procedure);

        if (procedure.ObjectType != Procedure.TypeClass)
        {
            throw new ArgumentException("The supplied object is not a procedure.", nameof(procedure));
        }

        // Parameters provide reliable types for SELECT expressions such as @CustomerId.
        var parameters = procedure.GetReferenced(Procedure.Parameters)
            .ToDictionary(x => x.Name.Parts.Last(), StringComparer.OrdinalIgnoreCase);

        // Restrict source lookup to DacPac dependencies so unrelated database objects cannot influence inference.
        var sources = procedure.GetReferenced(Procedure.BodyDependencies)
            .Where(x => x.IsAnyOfType(Table.TypeClass, View.TypeClass))
            .ToArray();

        // Column names can be resolved only when the referenced dependency exposes one unambiguous column.
        var columns = sources
            .SelectMany(GetColumns)
            .GroupBy(x => x.Name.Parts.Last(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.OrdinalIgnoreCase);
        // DacFx object ASTs do not consistently retain procedure bodies, so parse the persisted procedure script.
        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        var script = parser.Parse(new StringReader(procedure.GetScript()), out var errors);
        if (errors.Count > 0)
        {
            return [];
        }

        // ScriptDom exposes output SELECT statements separately from INSERT ... SELECT query sources.
        var statements = new SelectStatementVisitor();
        script.Accept(statements);

        // Keep script order: it determines result-set order for generated QueryMultipleAsync consumers.
        return statements.Statements
            .Where(IsResultProducing)
            .Select((statement, index) => Analyze(statement, index + 1, parameters, columns, sources))
            .ToArray();
    }

    /// <summary>
    /// Determines whether a SELECT statement returns scalar columns to the caller rather than populating a target table.
    /// </summary>
    private static bool IsResultProducing(SelectStatement statement)
    {
        return statement.Into is null
               && statement.QueryExpression is QuerySpecification { SelectElements.Count: > 0 } specification
               && specification.SelectElements.OfType<SelectScalarExpression>().Any();
    }

    /// <summary>
    /// Maps one result-producing SELECT statement into a result-set description.
    /// </summary>
    private static ProcedureResultSet Analyze(
        SelectStatement statement,
        int ordinal,
        IReadOnlyDictionary<string, TSqlObject> parameters,
        IReadOnlyDictionary<string, TSqlObject[]> columns,
        IReadOnlyList<TSqlObject> sources)
    {
        var specification = (QuerySpecification)statement.QueryExpression;
        var warnings = new List<string>();
        var results = specification.SelectElements
            .OfType<SelectScalarExpression>()
            .Select((element, index) => Analyze(element, index + 1, parameters, columns, warnings))
            .ToArray();

        if (results.Length != specification.SelectElements.Count)
        {
            warnings.Add("The result set contains a non-scalar SELECT element that could not be mapped.");
        }

        if (results.GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
        {
            warnings.Add("The result set contains duplicate column names; generated property names have been made unique.");
            results = results.Select((column, index) => column with { PropertyName = $"{column.PropertyName}{index + 1}" }).ToArray();
        }

        return new ProcedureResultSet(ordinal, GetPrimarySourceName(specification, sources), results, warnings);
    }

    /// <summary>
    /// Gets the columns exposed by a table or view dependency.
    /// </summary>
    private static IEnumerable<TSqlObject> GetColumns(TSqlObject source)
    {
        return source.ObjectType == Table.TypeClass
            ? source.GetReferenced(Table.Columns)
            : source.GetReferenced(View.Columns);
    }

    /// <summary>
    /// Resolves the first table or view named by the SELECT's FROM clause and returns its singular C# type name.
    /// </summary>
    private static string? GetPrimarySourceName(QuerySpecification specification, IReadOnlyList<TSqlObject> sources)
    {
        if (specification.FromClause is null)
        {
            return null;
        }

        var visitor = new PrimarySourceVisitor();
        specification.FromClause.Accept(visitor);
        var sourceName = visitor.SourceName;
        if (sourceName is null || !sources.Any(x => x.Name.Parts.Last().Equals(sourceName, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        return sourceName.Singularize().ToPascalCase();
    }

    /// <summary>
    /// Maps one scalar SELECT expression into a generated property and records any inference warning.
    /// </summary>
    private static ProcedureResultColumn Analyze(
        SelectScalarExpression element,
        int ordinal,
        IReadOnlyDictionary<string, TSqlObject> parameters,
        IReadOnlyDictionary<string, TSqlObject[]> columns,
        ICollection<string> warnings)
    {
        var name = element.ColumnName?.Value;
        if (string.IsNullOrWhiteSpace(name) && element.Expression is ColumnReferenceExpression columnReference)
        {
            name = columnReference.MultiPartIdentifier.Identifiers.LastOrDefault()?.Value;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Column{ordinal}";
            warnings.Add($"Column {ordinal} has no output name and was named '{name}'.");
        }

        var (type, warning) = GetType(element.Expression, parameters, columns);
        if (warning is not null)
        {
            warnings.Add($"Column '{name}': {warning}");
        }

        return new ProcedureResultColumn(name.ToPascalCase(), type);
    }

    /// <summary>
    /// Infers a C# type from a SQL scalar expression without guessing unsupported expression semantics.
    /// </summary>
    private static (string Type, string? Warning) GetType(
        ScalarExpression expression,
        IReadOnlyDictionary<string, TSqlObject> parameters,
        IReadOnlyDictionary<string, TSqlObject[]> columns)
    {
        switch (expression)
        {
            case CastCall cast:
                return GetSqlType(cast.DataType);
            case ConvertCall convert:
                return GetSqlType(convert.DataType);
            case IntegerLiteral:
                return ("int", null);
            case NumericLiteral:
            case MoneyLiteral:
                return ("decimal", null);
            case StringLiteral:
                return ("string", null);
            case NullLiteral:
                return ("object?", "NULL has no intrinsic SQL type.");
            case VariableReference variable when parameters.TryGetValue(variable.Name, out var parameter):
                return GetParameterType(parameter);
            case ColumnReferenceExpression columnReference:
                return GetColumnType(columnReference, columns);
            default:
                return ("object?", $"The SQL expression '{expression.GetType().Name}' could not be statically typed.");
        }
    }

    /// <summary>
    /// Maps a stored-procedure parameter's DacPac type and nullability to a C# type.
    /// </summary>
    private static (string Type, string? Warning) GetParameterType(TSqlObject parameter)
    {
        var dataType = parameter.GetReferenced(Parameter.DataType).FirstOrDefault();
        var type = dataType?.GetDotNetDataType(parameter.GetProperty<bool>(Parameter.IsNullable));
        return type is null
            ? ("object?", "The parameter data type could not be mapped.")
            : (type.ToString(), null);
    }

    /// <summary>
    /// Resolves a column reference when exactly one matching DacPac dependency column is available.
    /// </summary>
    private static (string Type, string? Warning) GetColumnType(
        ColumnReferenceExpression reference,
        IReadOnlyDictionary<string, TSqlObject[]> columns)
    {
        var name = reference.MultiPartIdentifier.Identifiers.LastOrDefault()?.Value;
        if (name is null || !columns.TryGetValue(name, out var matches) || matches.Length != 1)
        {
            return ("object?", "The referenced table column could not be resolved unambiguously from the DacPac model.");
        }

        var column = matches[0];
        var dataType = column.GetReferenced(Column.DataType).FirstOrDefault();
        var type = dataType?.GetDotNetDataType(column.GetProperty<bool>(Column.Nullable));
        return type is null
            ? ("object?", "The referenced column data type could not be mapped.")
            : (type.ToString(), null);
    }

    /// <summary>
    /// Maps an explicitly declared SQL data type used by CAST or CONVERT to a C# type.
    /// </summary>
    private static (string Type, string? Warning) GetSqlType(DataTypeReference dataType)
    {
        var name = dataType.Name.BaseIdentifier?.Value;
        var type = name is null ? null : ExtensionMethods.GetDotNetDataType(name);
        return type is null
            ? ("object?", $"The SQL type '{name ?? "unknown"}' could not be mapped.")
            : (type.ToString(), null);
    }

    /// <summary>
    /// Collects SELECT statements while preserving their traversal order in the procedure script.
    /// </summary>
    private sealed class SelectStatementVisitor : TSqlFragmentVisitor
    {
        /// <summary>
        /// Gets the SELECT statements encountered during traversal.
        /// </summary>
        public List<SelectStatement> Statements { get; } = [];

        /// <summary>
        /// Adds the encountered SELECT statement before traversing its child fragments.
        /// </summary>
        public override void ExplicitVisit(SelectStatement node)
        {
            Statements.Add(node);
            base.ExplicitVisit(node);
        }
    }

    /// <summary>
    /// Captures the first named table or view reference in a SELECT FROM clause.
    /// </summary>
    private sealed class PrimarySourceVisitor : TSqlFragmentVisitor
    {
        /// <summary>
        /// Gets the unqualified name of the first source encountered during traversal.
        /// </summary>
        public string? SourceName { get; private set; }

        /// <summary>
        /// Captures the source name before traversing joined or nested table references.
        /// </summary>
        public override void ExplicitVisit(NamedTableReference node)
        {
            SourceName ??= node.SchemaObject.BaseIdentifier?.Value;
            base.ExplicitVisit(node);
        }
    }
}

/// <summary>
/// Describes one result set returned by a stored procedure SELECT statement.
/// </summary>
/// <param name="Ordinal">The one-based result-set position in script order.</param>
/// <param name="SourceName">The singular C# type name inferred from the primary table or view, when available.</param>
/// <param name="Columns">The mapped output columns for the result set.</param>
/// <param name="Warnings">Details that could not be statically determined during analysis.</param>
public sealed record ProcedureResultSet(
    int Ordinal,
    string? SourceName,
    IReadOnlyList<ProcedureResultColumn> Columns,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Describes the generated C# property for one stored-procedure result column.
/// </summary>
/// <param name="PropertyName">The PascalCase property name emitted into the generated result type.</param>
/// <param name="Type">The inferred C# type emitted for the property.</param>
public sealed record ProcedureResultColumn(string PropertyName, string Type);
