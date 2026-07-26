using System.Text;
using System.Security;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

public class ViewToCsharpClassGenerator : CsharpGenerator
{
    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents a {sqlObject.Name.Parts.Last()} {sqlObject.Name}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("/// The generated view is not 100% reliable because not all result types can be derived from the DACPAC.");
        sb.AppendLine("/// If the database is installed, retrieve the result types with");
        sb.AppendLine($"/// <code>EXEC sys.sp_describe_first_result_set N'SELECT * FROM {sqlObject.Name}';</code>");
        sb.AppendLine("/// <code>");
        foreach (var line in sqlObject.GetScript().Split('\n'))
        {
            sb.AppendLine($"/// {SecurityElement.Escape(line.TrimEnd('\r'))}");
        }

        sb.AppendLine("/// </code>");
        sb.AppendLine("/// </remarks>");

        sb.AppendLine($"public class {sqlObject.Name.Parts.Last().ToPascalCase()}");
        sb.AppendLine("{");
        
        BuildProperties(sqlObject, sb);

        sb.AppendLine("}");
    }

    private void BuildProperties(TSqlObject sqlObject, StringBuilder sb)
    {
        ViewSelectStatementAnalyzer analyzer = new ViewSelectStatementAnalyzer();
        foreach (var viewSelectColumn in analyzer.Analyze(sqlObject))
        {
            GeneratePropertyWithSummary(viewSelectColumn, sb);
        }
    }
    
    /// <summary>
    /// Writes the XML documentation and C# type declaration for a table column.
    /// </summary>
    private StringBuilder GeneratePropertyWithSummary(ViewSelectColumn column, StringBuilder sb)
    {
        var columnName = column.ColumnObject.Name.Parts.Last();
        var dataType = column.GuessType();
        var isNullable = column.ColumnObject.GetProperty<bool>(Column.Nullable);
        

        sb.AppendLine($"""
                       /// <summary>
                       /// Gets or sets the {columnName} ({dataType.property?.Name.ToString()}){(isNullable ? " (nullable)" : "")}.
                       /// </summary>
                       """);

        sb.AppendLine("///<remarks>");
        if (dataType is {guessed: true})
        {
            if (dataType.property is not null)
            {
                sb.AppendLine($"///The type has been guessed. Projection {column.Expression}");
            }
            else
                sb.AppendLine($"/// The type could not be guessed. Projection {column.Expression}");

        }

        sb.AppendLine("/// </remarks>");

        var dotnetType = dataType.property;
        if (dotnetType == null)
        {
            sb.AppendLine($"// Warning: Unrecognized SQL data type '{dataType.property?.Name.ToString()}' for column '{columnName}'.");
            sb.AppendLine($"public UNRESOLVED {columnName.ToPascalCase()} {{ get; set; }}");
        }
        else
        {
            sb.AppendLine($"public {dotnetType} {columnName.ToPascalCase()} {{ get; set; }}");
        }

        return sb;
    }

    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == View.TypeClass && tSqlObject.Name.HasName;
    }
}
