using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public partial class TableTypeDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<TableTypeColumnDisplay> Columns { get; set; }

    public TableTypeDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(TableType.TypeClass);

        Columns = [.. Model.GetReferenced(TableType.Columns).Select(column => new TableTypeColumnDisplay(column))];
    }

    protected override bool FilterReferenced(TSqlObject arg)
    {
        if (arg.ObjectType == TableTypeColumn.TypeClass)
            return false;

        return base.FilterReferenced(arg);
    }
}
