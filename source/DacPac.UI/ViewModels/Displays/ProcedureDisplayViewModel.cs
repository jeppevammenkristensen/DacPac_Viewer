using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.UI.Infrastructure;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public partial class ProcedureDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<ParameterWrapper> Parameters { get; set; }

    public ProcedureDisplayViewModel(TSqlObject model) : base(model)
    {
        if (model.ObjectType != Procedure.TypeClass)
        {
            throw new InvalidOperationException($"The provided TSqlObject is not a procedure. Expected type: {Procedure.TypeClass.Name}, but got: {model.ObjectType.Name}");
        }

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
