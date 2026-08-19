using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Exposes display data for a view output column.
/// </summary>
public class ViewColumnWrapper
{
    /// <summary>Gets the analyzed view column.</summary>
    public ViewSelectColumn Model { get; }

    public ViewColumnWrapper(ViewSelectColumn model)
    {
        Model = model;
        ColumnName = model.ColumnObject.Name.Parts.Last();

        if (model.ColumnObject.GetReferenced(Column.DataType).ToList() is { Count: 1 } dataType)
        {
            Type = dataType[0].Name.Parts.Last();
        }
        else if (model.ColumnObject.GetReferencedRelationshipInstances(DacExternalQueryScopes.All).ToList() is
                 { Count: > 0 } referenced)
        {
            Type = referenced[0].Object.GetReferenced(Column.DataType).FirstOrDefault()?.Name.Parts.Last();
        }

        Expression = model.Expression;
    }

    /// <summary>Gets or sets the SQL expression that produces the column.</summary>
    public string Expression { get; set; }
    /// <summary>Gets or sets the column name.</summary>
    public string ColumnName { get; set; }
    /// <summary>Gets or sets the SQL data type name.</summary>
    public string? Type { get; set; }
}
