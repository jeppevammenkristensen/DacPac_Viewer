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

        var parameters = new StringBuilder(); 
        var (_, hasParameters) = BuildParametersObject(sqlObject, parameters);
        
        sb.Append("public async Task ExecuteAsync(");
        if (hasParameters)
        {
            sb.Append($"Parameters parameters");
        }
        sb.AppendLine(")");
        sb.AppendLine("{");

        sb.AppendLine($"var procedureName = \"{sqlObject.Name.ToString()}\";");
        sb.AppendLine("}");
        
        sb.AppendLine(parameters.ToString());
        
        sb.AppendLine("}");
    }

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

            sb.AppendLine("}");
            return (sb, true);
        }

        return (sb, false);
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