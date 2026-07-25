using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public sealed class ColumnWrapper
{
    public string ColumnName { get;  }
    public bool IsNullable { get;  }
    
    public bool IsIdentity { get; }
    
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