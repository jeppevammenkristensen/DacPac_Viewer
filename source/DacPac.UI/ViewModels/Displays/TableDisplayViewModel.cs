using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public interface IDisplayViewModel 
{
    
}

public partial class TableDisplayViewModel : ViewModelBase, IDisplayViewModel
{
    [ObservableProperty] public partial string ShortName { get; set; }
    [ObservableProperty] public partial string FullName { get; set; }
    [ObservableProperty] public partial ObservableCollection<TableColumnWrapper> Columns { get; set; }
    [ObservableProperty] public partial string Script { get; set; }
    [ObservableProperty] public partial ObservableCollection<string> Referencing { get; set; }
    [ObservableProperty] public partial IEnumerable<string> Referenced { get; set; }

    public TableDisplayViewModel(TSqlObject model)
    {
        if (model.ObjectType != Table.TypeClass)
        {
            throw new InvalidOperationException($"The provided TSqlObject is not a table. Expected type: {Table.TypeClass.Name}, but got: {model.ObjectType.Name}");
        }

        ShortName = model.Name.Parts.Last();
        FullName = model.Name.ToString();
        Columns = [..model.GetReferenced(Table.Columns).Select(x => new TableColumnWrapper(x))];
        Script = model.GetScript();
        Referencing = [..model.GetReferencing().Select(x => x.Name.ToString())];
        Referenced = model.GetReferenced().Select(x => x.Name.ToString());
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
