using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public sealed class TableTypeColumnDisplay
{
    public string ColumnName { get; }
    public bool IsNullable { get; }
    public bool IsIdentity { get; }
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
