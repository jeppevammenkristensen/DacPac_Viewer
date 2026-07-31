using System.Text;
using DacPac.Wrappers;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

/// <summary>
/// Generates a C# data class from a DacPac table definition.
/// </summary>
public class TableToCsharpClassGenerator : CsharpGenerator
{
    public override string TypeName(TSqlObject sqlObject)
    {
        return sqlObject.GenerateTypeName("Table");
    }

    /// <summary>
    /// Writes a class and mapped properties for the supplied table.
    /// </summary>
    protected override void DoBuild(TSqlObject sqlObject,StringBuilder sb)
    {
        sb.AppendSummary($"Represents the table {sqlObject.Name.Parts.Last()} {sqlObject.Name}.");
        sb.AppendClass(TypeName(sqlObject));
        
        BuildProperties(sqlObject, sb);

        sb.AppendLine("}");

    }

    /// <summary>
    /// Writes one generated property for each table column.
    /// </summary>
    private void BuildProperties(TSqlObject sqlObject, StringBuilder sb)
    {
        var foreignKeyConstraintWrappers = sqlObject.GetReferencingRelationshipInstances(DacQueryScopes.All)
            .Select(x => x.FromObject)
            .Where(x => x.ObjectType == ForeignKeyConstraint.TypeClass)
            .Select(x => x.ToForeignKeyConstraint())
            .ToList();

        var hostedForeignKeyConstraints = foreignKeyConstraintWrappers
            .Where(x => x.Host.Any(y => y.Name.ToString() == sqlObject.Name.ToString()))
            .ToList();

        var referencingForeignKeyConstraints = foreignKeyConstraintWrappers
            .Where(x => x.ForeignTable.Any(y => y.Name.ToString() == sqlObject.Name.ToString()))
            .ToList();

        foreach (var column in sqlObject.GetReferenced(Table.Columns))
        {
            GeneratePropertyWithSummary(column, sb, hostedForeignKeyConstraints, referencingForeignKeyConstraints); 
        }
    }

    private void GeneratePropertyWithSummary(TSqlObject column, StringBuilder sb, List<ForeignKeyConstraintWrapper> hostedForeignKeyConstraints, List<ForeignKeyConstraintWrapper> referencingForeignKeyConstraints)
    {
        var columnName = column.Name.Parts.Last();
        var dataType = column.GetReferenced(Column.DataType).FirstOrDefault();
        var isNullable = column.GetProperty<bool>(Column.Nullable);
        var remarks = string.Join(
            Environment.NewLine,
            hostedForeignKeyConstraints
                .Where(x => x.Columns.Any(y => y.Name.Parts.Last() == columnName))
                .Select(x => $"Foreign key pointing to {x.ForeignColumns.Select(y => y.Name.ToString()).First()} in {x.ForeignTable.First().Name}")
                .Concat(referencingForeignKeyConstraints
                .Where(x => x.ForeignColumns.Any(y => y.Name.Parts.Last() == columnName))
                .Select(x => $"Key referenced by {x.Columns.Select(y => y.Name.ToString()).First()} in {x.Host.First().Name}")));

        sb.AppendSummary(
            $"Gets or sets the {columnName} ({dataType?.Name}){(isNullable ? " (nullable)" : "")}.",
            remarks);
        
        var dotnetType = dataType == null ? null : ExtensionMethods.GetDotNetDataType(dataType, isNullable);
        if (dotnetType == null)
        {
            sb.AppendLine($"// Warning: Unrecognized SQL data type '{dataType?.Name.ToString()}' for column '{columnName}'.");
            sb.AppendProperty("object", columnName.ToPascalCase());
        }
        else
        {
            sb.AppendProperty(dotnetType.ToString(), columnName.ToPascalCase());
        }

    }

    

    /// <summary>
    /// Determines whether the object is a named table.
    /// </summary>
    public override bool IsValid(TSqlObject tSqlObject)
    {
        return tSqlObject.ObjectType == Table.TypeClass && tSqlObject.Name.HasName;
    }
}
