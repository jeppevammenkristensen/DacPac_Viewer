using System.Text;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

public class ProcedureToClassGenerator : CsharpGenerator
{
    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        sb.AppendLine($"""
                       /// <summary>
                       /// Represents a {sqlObject.Name.Parts.Last()} {sqlObject.Name.ToString()}
                       /// </summary>
                       """);
        sb.AppendLine($"public class {sqlObject.Name.Parts.Last().ToPascalCase()}Procedure");
        sb.AppendLine("{");
        
        //BuildProperties(sqlObject, sb);

        sb.AppendLine("}");
    }

    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == Procedure.TypeClass && tSqlObject.Name.HasName;
    }
}


public class TableToCsharpClassGenerator : CsharpGenerator
{
    protected override void DoBuild(TSqlObject sqlObject,StringBuilder sb)
    {
        sb.AppendLine($"""
                       /// <summary>
                       /// Represents a {sqlObject.Name.Parts.Last()} {sqlObject.Name.ToString()}
                       /// </summary>
                       """);
        sb.AppendLine($"public class {sqlObject.Name.Parts.Last().ToPascalCase()}");
        sb.AppendLine("{");
        
        BuildProperties(sqlObject, sb);

        sb.AppendLine("}");

    }

    private void BuildProperties(TSqlObject sqlObject, StringBuilder sb)
    {
        foreach (var column in sqlObject.GetReferenced(Table.Columns))
        {
            GeneratePropertyWithSummary(column, sb); 
        }
    }

    private StringBuilder GeneratePropertyWithSummary(TSqlObject column, StringBuilder sb)
    {
        var columnName = column.Name.Parts.Last();
        var dataType = column.GetReferenced(Column.DataType).FirstOrDefault();
        var isNullable = column.GetProperty<bool>(Column.Nullable);
        var isIdentity = column.GetProperty<bool>(Column.IsIdentity);
        var max = column.GetProperty<int>(Column.Length);
        

        sb.AppendLine($"""
                       /// <summary>
                       /// Gets or sets the {columnName} ({dataType?.Name.ToString()}){(isNullable ? " (nullable)" : "")}.
                       /// </summary>
                       """);
        
        var dotnetType = dataType == null ? null : ExtensionMethods.GetDotNetDataType(dataType, isNullable);
        if (dotnetType == null)
        {
            sb.AppendLine($"// Warning: Unrecognized SQL data type '{dataType}' for column '{columnName}'.");
            sb.AppendLine($"public object {columnName.ToPascalCase()} {{ get; set; }}");
        }
        else
        {
            sb.AppendLine($"public {dotnetType} {columnName.ToPascalCase()} {{ get; set; }}");
        }

        return sb;
    }

    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == Table.TypeClass && tSqlObject.Name.HasName;
    }
}