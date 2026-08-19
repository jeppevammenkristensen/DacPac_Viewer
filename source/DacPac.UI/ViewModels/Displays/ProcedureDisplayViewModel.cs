using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public partial class ProcedureDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<ParameterWrapper> Parameters { get; set; }

    public ProcedureDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(Procedure.TypeClass);
        Parameters = [..Model.GetReferenced(Procedure.Parameters).Select(x => new ParameterWrapper(x))];
    }
}
