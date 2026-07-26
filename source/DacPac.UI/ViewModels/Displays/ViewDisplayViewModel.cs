using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

public partial class ViewDisplayViewModel : DisplayViewModel
{
    private static readonly ViewSelectStatementAnalyzer SelectStatementAnalyzer = new();
    private const string UnavailableExpression = "Expression unavailable";

    [ObservableProperty] public partial ObservableCollection<ViewColumnWrapper> ColumnWrappers { get; set; }

    public ViewDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(View.TypeClass);

        try
        {
            ColumnWrappers = [.. SelectStatementAnalyzer.Analyze(model).Select(x => new ViewColumnWrapper(x))];
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
        {
            // View metadata remains useful even when the SQL expression cannot be mapped.
            ColumnWrappers =
            [
                .. model.GetReferenced(View.Columns)
                    .Select(column => new ViewColumnWrapper(new ViewSelectColumn(column, UnavailableExpression, null)))
            ];
        }
    }
}

public class ViewColumnWrapper
{
    public ViewSelectColumn Model { get; }
    
    public ViewColumnWrapper(ViewSelectColumn model)
    {
        Model = model;
        ColumnName = model.ColumnObject.Name.Parts.Last();

        if (model.ColumnObject.GetReferenced(Column.DataType).ToList() is {Count: 1} dataType)
        {
            Type = dataType[0].Name.Parts.Last();
        }
        else if (model.ColumnObject.GetReferencedRelationshipInstances(DacExternalQueryScopes.All).ToList() is
                 {Count: > 0} referenced)
        {
            Type = referenced[0].Object.GetReferenced(Column.DataType).FirstOrDefault()?.Name.Parts.Last();
        }

        Expression = model.Expression;

    }

    public string Expression { get; set; }

    public string ColumnName { get; set; }

    public string? Type { get; set; }
}

