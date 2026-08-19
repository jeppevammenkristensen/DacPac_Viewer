using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Exposes display data for a stored-procedure parameter.
/// </summary>
public class ParameterWrapper
{
    /// <summary>Gets the parameter name.</summary>
    public string ColumnName { get; }
    /// <summary>Gets whether the parameter accepts null values.</summary>
    public bool IsNullable { get; }
    /// <summary>Gets or sets the SQL data type name.</summary>
    public string? Type { get; set; }

    public ParameterWrapper(TSqlObject sqlObject)
    {
        ColumnName = sqlObject.Name.Parts.Last();
        IsNullable = sqlObject.GetProperty<bool>(Parameter.IsNullable);
        Type = sqlObject.GetReferenced(Parameter.DataType).FirstOrDefault()?.Name.Parts.Last();
    }
}
