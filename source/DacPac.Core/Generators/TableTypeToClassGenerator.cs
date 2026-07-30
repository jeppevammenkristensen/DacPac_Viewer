using System.Text;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

public class TableTypeToClassGenerator : CsharpGenerator
{
    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        var tableTypeWrapper = sqlObject.ToTableType();
        sb.AppendLine($"""
                       /// <summary>
                       /// Represents the table {sqlObject.Name.Parts.Last()} {sqlObject.Name.ToString()}
                       /// </summary>
                       """);
        sb.AppendLine($"public class {sqlObject.Name.Parts.Last().ToPascalCase()}");
        sb.AppendLine("{");
        
        BuildProperties(tableTypeWrapper, sb);
        
        sb.AppendLine("}");
        
    }

    private void BuildProperties(TableTypeWrapper tableTypeWrapper, StringBuilder sb)
    {
        foreach (var sqlObject in tableTypeWrapper.Columns)
        {
            foreach (var column in sqlObject.GetReferenced(Table.Columns).Select(x => x.ToColumn()))
            {
                GeneratePropertyWithSummary(column, sb); 
            }
        }
    }
    
     private StringBuilder GeneratePropertyWithSummary(ColumnWrapper column, StringBuilder sb)
     {
          var columnName = column.SqlObject.Name.ToString();
        
         var dataType = column.SqlObject.GetReferenced(Column.DataType).FirstOrDefault();
         var isNullable = column.SqlObject.GetProperty<bool>(Column.Nullable);
        

        sb.AppendLine($"""
                       /// <summary>
                       /// Gets or sets the {columnName} ({dataType?.Name.ToString()}){(isNullable ? " (nullable)" : "")}.
                       /// </summary>
                       """);
        
        var dotnetType = dataType == null ? null : ExtensionMethods.GetDotNetDataType(dataType, isNullable);
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
