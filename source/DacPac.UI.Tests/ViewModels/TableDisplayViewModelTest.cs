using System.Linq;
using DacPac.UI.ViewModels.Displays;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.ViewModels;

public class TableDisplayViewModelTest
{
    [Fact]
    public void FilterReferenced_ExcludesColumns()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);");
        var table = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();
        var column = model.GetObjects(DacQueryScopes.UserDefined, Column.TypeClass).Single();
        var viewModel = new TestTableDisplayViewModel(table);

        Assert.False(viewModel.Includes(column));
        Assert.True(viewModel.Includes(table));
    }

    private sealed class TestTableDisplayViewModel(TSqlObject model) : TableDisplayViewModel(model)
    {
        public bool Includes(TSqlObject sqlObject) => FilterReferenced(sqlObject);
    }
}
