using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.LandingPage.Displays;

public interface IDisplayViewModel 
{
    
}

public partial class TableDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<ColumnWrapper> Columns { get; set; }

    public TableDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(Table.TypeClass);

        Columns = [..Model.GetReferenced(Table.Columns).Select(x => new ColumnWrapper(x))];
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
            return false;

        return base.FilterReferenced(arg);
    }
}
