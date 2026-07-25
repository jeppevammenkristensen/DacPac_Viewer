using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DacPac.UI.ViewModels.Displays;

public partial class ViewDisplayViewModel : DisplayViewModel
{
    [ObservableProperty] public partial ObservableCollection<ViewColumnWrapper> ColumnWrappers { get; set; }

    public ViewDisplayViewModel(TSqlObject model) : base(model)
    {
        model.ThrowIfIncorrectType(View.TypeClass);

        var selectColumnMappings = AnalyzeSelectStatement(model).ToList();

        ColumnWrappers =  [..selectColumnMappings.Select(x => new ViewColumnWrapper(x))];
    }

    private IEnumerable<SelectColumnMapping> AnalyzeSelectStatement(TSqlObject model)
    {
        var sql = model.GetProperty(View.SelectStatement).ToString();

        var parser = new TSql170Parser(initialQuotedIdentifiers: false);
        using var reader = new StringReader(sql);

        var script = (TSqlScript)parser.Parse(reader, out var errors);

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));

        var createView = script.Batches
            .SelectMany(batch => batch.Statements)
            .OfType<SelectStatement>()
            .Single();

        var query = (QuerySpecification)createView.QueryExpression;

        var outputColumns = model.GetReferenced(View.Columns).ToList();
        var selectExpressions = query.SelectElements
            .OfType<SelectScalarExpression>()
            .ToList();

        var results = outputColumns.Zip(selectExpressions, (column, selected) =>
        {
            var function = selected.Expression as FunctionCall;

            return new SelectColumnMapping(column, GetText(selected.Expression, sql), function is null, function?.FunctionName.Value, function?.Parameters
                     .Select(parameter => GetText(parameter, sql))
                     .ToArray());
        });

        return results;
    }
    
    
    
    static string GetText(TSqlFragment fragment, string sql)
    {
        return sql.Substring(fragment.StartOffset, fragment.FragmentLength);
    }
}

public record SelectColumnMapping(
    TSqlObject Column,
    string Expression,
    bool IsFunction,
    string? FunctionName,
    string[]? Arguments);

public class ViewColumnWrapper
{
    public SelectColumnMapping Model { get; }
    
    public ViewColumnWrapper(SelectColumnMapping model)
    {
        Model = model;
        ColumnName = model.Column.Name.Parts.Last();

        if (model.Column.GetReferenced(Column.DataType).ToList() is {Count: 1} dataType)
        {
            Type = dataType[0].Name.Parts.Last();
        }
        else if (model.Column.GetReferencedRelationshipInstances(DacExternalQueryScopes.All).ToList() is
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

