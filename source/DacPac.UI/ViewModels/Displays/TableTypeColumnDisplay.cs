using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Exposes display data for a table-type column.
/// </summary>
public sealed class TableTypeColumnDisplay
{
    /// <summary>Gets the column name.</summary>
    public string ColumnName { get; }
    /// <summary>Gets whether the column accepts null values.</summary>
    public bool IsNullable { get; }
    /// <summary>Gets whether the column is an identity column.</summary>
    public bool IsIdentity { get; }
    /// <summary>Gets the SQL data type name.</summary>
    public string? Type { get; }

    public TableTypeColumnDisplay(TSqlObject sqlObject)
    {
        sqlObject.ThrowIfIncorrectType(TableTypeColumn.TypeClass);
        ColumnName = sqlObject.Name.Parts.Last();
        IsNullable = sqlObject.GetProperty<bool>(TableTypeColumn.Nullable);
        IsIdentity = sqlObject.GetProperty<bool>(TableTypeColumn.IsIdentity);
        Type = sqlObject.GetReferenced(TableTypeColumn.DataType).FirstOrDefault()?.Name.Parts.Last();
    }
}
