using System.Text;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

public class TableTypeToClassGenerator : CsharpGenerator
{
    public override string TypeName(TSqlObject sqlObject)
    {
        return sqlObject.GenerateTypeName("TableType");
    }

    public override ModelTypeClass[] SupportedObjectTypes => [TableType.TypeClass];

    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        var tableTypeWrapper = sqlObject.ToTableType();
        var className = TypeName(sqlObject);
        sb.AppendLine($"""
                       /// <summary>
                       /// Represents the table {sqlObject.Name.Parts.Last()} {sqlObject.Name.ToString()}
                       /// </summary>
                       """);
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");
        
        BuildProperties(tableTypeWrapper, sb);
        BuildToDataTable(tableTypeWrapper, className, sb);
        
        sb.AppendLine("}");
        
    }

    private void BuildProperties(TableTypeWrapper tableTypeWrapper, StringBuilder sb)
    {
        foreach (var column in tableTypeWrapper.Columns.Select(x => x.ToTableTypeColumn()))
        {
            GeneratePropertyWithSummary(column, sb); 
        }
    }

    /// <summary>
    /// Writes a conversion method that maps generated instances into a table-valued parameter data table.
    /// </summary>
    private static void BuildToDataTable(TableTypeWrapper tableTypeWrapper, string className, StringBuilder sb)
    {
        var columns = tableTypeWrapper.Columns.Select(x => x.ToTableTypeColumn()).ToList();

        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Creates a data table containing the supplied rows for use as a table-valued parameter.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static System.Data.DataTable ToDataTable(System.Collections.Generic.IEnumerable<{className}> rows)");
        sb.AppendLine("{");
        sb.AppendLine("var table = new System.Data.DataTable();");

        foreach (var column in columns)
        {
            var columnName = column.SqlObject.Name.Parts.Last();
            var dotnetType = column.DataType.FirstOrDefault()?.GetDotNetDataType(column.Nullable)?.ToString() ?? "object";
            var nonNullableType = dotnetType.TrimEnd('?');
            sb.AppendLine($"table.Columns.Add(\"{columnName}\", typeof({nonNullableType}));");
        }

        sb.AppendLine("foreach (var item in rows)");
        sb.AppendLine("{");
        sb.AppendLine("table.Rows.Add(");
        foreach (var column in columns.Index())
        {
            var propertyName = column.Item.SqlObject.Name.Parts.Last().ToPascalCase();
            sb.AppendLine($"(object?)item.{propertyName} ?? System.DBNull.Value{(column.Index < columns.Count - 1 ? "," : string.Empty)}");
        }
        sb.AppendLine(");");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("return table;");
        sb.AppendLine("}");
    }
    
     private StringBuilder GeneratePropertyWithSummary(TableTypeColumnWrapper column, StringBuilder sb)
     {
         var columnName = column.SqlObject.Name.Parts.Last();
        
         var dataType = column.DataType.FirstOrDefault();
         var isNullable = column.Nullable;

        sb.AppendLine($"""
                       /// <summary>
                       /// Gets or sets the {columnName} ({dataType?.Name.ToString()}){(isNullable ? " (nullable)" : "")}.
                       /// </summary>
                       """);
        
        var dotnetType = dataType?.GetDotNetDataType(isNullable);
        if (dotnetType == null)
        {
            sb.AppendLine($"// Warning: Unrecognized SQL data type '{dataType?.Name.ToString()}' for column '{columnName}'.");
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
        return tSqlObject.ObjectType == TableType.TypeClass;
    }
}
