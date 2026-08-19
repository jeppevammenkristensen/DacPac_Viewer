using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Exposes display data for a table column.
/// </summary>
public sealed class ColumnWrapper
{
    /// <summary>Gets the column name.</summary>
    public string ColumnName { get;  }
    /// <summary>Gets whether the column accepts null values.</summary>
    public bool IsNullable { get;  }
    
    /// <summary>Gets whether the column is an identity column.</summary>
    public bool IsIdentity { get; }
    
    /// <summary>Gets or sets the SQL data type name.</summary>
    public string? Type { get; set; }

    public ColumnWrapper(TSqlObject sqlObject)
    {
        sqlObject.ThrowIfIncorrectType(Column.TypeClass);
        
        ColumnName = sqlObject.Name.Parts.Last();
        IsNullable = sqlObject.GetProperty<bool>(Column.Nullable);
        IsIdentity = sqlObject.GetProperty<bool>(Column.IsIdentity);
        Type = sqlObject.GetReferenced(Column.DataType).FirstOrDefault()?.Name.Parts.Last();
    }
}
