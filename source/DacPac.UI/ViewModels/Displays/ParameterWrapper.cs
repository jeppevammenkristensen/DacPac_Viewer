using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public class ParameterWrapper
{
    public string ColumnName { get; }
    public bool IsNullable { get; }
    public string? Type { get; set; }

    public ParameterWrapper(TSqlObject sqlObject)
    {
        ColumnName = sqlObject.Name.Parts.Last();
        IsNullable = sqlObject.GetProperty<bool>(Parameter.IsNullable);
        Type = sqlObject.GetReferenced(Parameter.DataType).FirstOrDefault()?.Name.Parts.Last();
    }
}
