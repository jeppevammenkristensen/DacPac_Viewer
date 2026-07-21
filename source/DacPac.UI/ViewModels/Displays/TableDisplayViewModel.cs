using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public interface IDisplayViewModel 
{
    
}

public partial class TableDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<TableColumnWrapper> Columns { get; set; }

    public TableDisplayViewModel(TSqlObject model) : base(model)
    {
        if (model.ObjectType != Table.TypeClass)
        {
            throw new InvalidOperationException($"The provided TSqlObject is not a table. Expected type: {Table.TypeClass.Name}, but got: {model.ObjectType.Name}");
        }

        Columns = [..Model.GetReferenced(Table.Columns).Select(x => new TableColumnWrapper(x))];
    }

    /// <summary>
    /// Filters the referenced objects for the table display view model.
    /// Specifically, excludes column objects from the referenced list.
    /// </summary>
    /// <param name="arg">The TSqlObject to be evaluated for filtering.</param>
    /// <returns>True if the object should be included in the referenced list; otherwise, false.</returns>
    protected override bool FilterReferenced(TSqlObject arg)
    {
        if (arg.ObjectType == Column.TypeClass)
        {
            return false; // Exclude columns from the referenced list
        }
        return base.FilterReferenced(arg);
    }
}

public class TableColumnWrapper
{
    public string ColumnName { get;  }
    public bool IsNullable { get;  }
    
    public bool IsIdentity { get; }
    
    public string? Type { get; set; }

    public TableColumnWrapper(TSqlObject sqlObject)
    {
        ColumnName = sqlObject.Name.Parts.Last();
        IsNullable = sqlObject.GetProperty<bool>(Column.Nullable);
        IsIdentity = sqlObject.GetProperty<bool>(Column.IsIdentity);
        Type = sqlObject.GetReferenced(Column.DataType).FirstOrDefault()?.Name.Parts.Last();
    }
    
    
}
