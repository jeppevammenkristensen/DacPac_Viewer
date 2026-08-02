using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.LandingPage.Displays;

public partial class ProcedureDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<ParameterWrapper> Parameters { get; set; }

    public ProcedureDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(Procedure.TypeClass);
        Parameters = [..Model.GetReferenced(Procedure.Parameters).Select(x => new ParameterWrapper(x))];
    }
}

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
