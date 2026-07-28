using System.Text;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

/// <summary>
/// Generates a C# data class from a DacPac table definition.
/// </summary>
public class TableToCsharpClassGenerator : CsharpGenerator
{
    /// <summary>
    /// Writes a class and mapped properties for the supplied table.
    /// </summary>
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

    /// <summary>
    /// Writes one generated property for each table column.
    /// </summary>
    private void BuildProperties(TSqlObject sqlObject, StringBuilder sb)
    {
        var foreignKeyConstraintWrappers = sqlObject.GetReferencingRelationshipInstances()
            .Select(x => x.FromObject)
            .Where(x => x.ObjectType == ForeignKeyConstraint.TypeClass)
            .Select(x => x.ToForeignKeyConstraint()).ToList();

        var hostedForeignKeyConstraints = foreignKeyConstraintWrappers
            .Where(x => x.Host.Any(y => y.Name.ToString() == sqlObject.Name.ToString()))
            .ToList();

        var referencingForeignKeyConstraints = foreignKeyConstraintWrappers.Where(x =>
            x.ForeignTable.Any(y => y.Name.ToString() == sqlObject.Name.ToString())).ToList();
        
        foreach (var column in sqlObject.GetReferenced(Table.Columns))
        {
            GeneratePropertyWithSummary(column, sb, hostedForeignKeyConstraints, referencingForeignKeyConstraints); 
        }
    }

    private StringBuilder GeneratePropertyWithSummary(TSqlObject column, StringBuilder sb, List<ForeignKeyConstraintWrapper> hostedForeignKeyConstraints, List<ForeignKeyConstraintWrapper> referencingForeignKeyConstraints)
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
        
        sb.AppendLine("///<remarks>");
        foreach (var foreignKeyConstraintWrapper in hostedForeignKeyConstraints.Where(x => x.Columns.Any(y => y.Name.ToString() == column.Name.ToString())))
        {
            sb.AppendLine(
                $"/// Foreign key pointing to {foreignKeyConstraintWrapper.ForeignColumns.Select(x => x.Name.ToString()).First()} in {foreignKeyConstraintWrapper.ForeignTable.First().Name.ToString()}");
        }

        foreach (var referencingForeignKeyConstraint in referencingForeignKeyConstraints.Where(x => x.ForeignColumns.Any(y => y.Name.ToString() == column.Name.ToString())))
        {
            sb.AppendLine($"/// Key referenced by {referencingForeignKeyConstraint.Columns.Select(x => x.Name.ToString()).First()} in {referencingForeignKeyConstraint.Host.First().Name.ToString()}");
        }
        
        sb.AppendLine("/// </remarks>");
        
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

    

    /// <summary>
    /// Determines whether the object is a named table.
    /// </summary>
    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == Table.TypeClass && tSqlObject.Name.HasName;
    }
}
