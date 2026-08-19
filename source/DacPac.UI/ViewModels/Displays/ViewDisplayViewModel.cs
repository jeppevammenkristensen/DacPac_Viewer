using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.Displays;

/// <summary>
/// Provides display data for a database view.
/// </summary>
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
