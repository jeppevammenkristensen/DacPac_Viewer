using System.Collections.Generic;
using System.Linq;
using DacPac.UI.Models.LandingPage;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Models.LandingPage;

public class SqlObjectExtensionsTest
{
    [Fact]
    public void GetTreeIcon_ReturnsExpectedIconForSupportedObjectTypes()
    {
        using var model = CreateModel("""
            CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);
            GO
            CREATE VIEW [dbo].[CustomerView] AS SELECT [Id] FROM [dbo].[Customer];
            GO
            CREATE PROCEDURE [dbo].[GetCustomer] @CustomerId int AS SELECT @CustomerId;
            """);
        var table = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();
        var column = table.GetReferenced(Table.Columns).Single();
        var view = model.GetObjects(DacQueryScopes.UserDefined, View.TypeClass).Single();
        var procedure = model.GetObjects(DacQueryScopes.UserDefined, Procedure.TypeClass).Single();
        var parameter = procedure.GetReferenced(Procedure.Parameters).Single();

        Assert.Equal(TreeIconIds.Table, table.GetTreeIcon());
        Assert.Equal(TreeIconIds.Column, column.GetTreeIcon());
        Assert.Equal(TreeIconIds.View, view.GetTreeIcon());
        Assert.Equal(TreeIconIds.Procedure, procedure.GetTreeIcon());
        Assert.Equal(TreeIconIds.Parameter, parameter.GetTreeIcon());
    }

    [Fact]
    public void GetReferencedAndReferencing_OmitsEmptyFolders()
    {
        using var model = CreateModel("CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);");
        var table = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();

        var folders = new TestTreeItem(table).GetReferencedAndReferencing(_ => false).ToList();

        Assert.Empty(folders);
    }

    [Fact]
    public void GetReferencedAndReferencing_GroupsReferencedAndReferencingObjects()
    {
        using var model = CreateModel("""
            CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);
            GO
            CREATE VIEW [dbo].[CustomerView] AS SELECT [Id] FROM [dbo].[Customer];
            """);
        var table = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass).Single();

        var folders = new TestTreeItem(table).GetReferencedAndReferencing(x => x.ObjectType != Column.TypeClass).ToList();

        var referenced = Assert.IsType<FolderTreeItem>(Assert.Single(folders, x => x.Name == "Referenced"));
        var referencedBy = Assert.IsType<FolderTreeItem>(Assert.Single(folders, x => x.Name == "Referenced by"));
        var referencedGroup = Assert.IsType<TypeGroupTreeItem>(Assert.Single(referenced.Children));
        var referencingGroup = Assert.IsType<TypeGroupTreeItem>(Assert.Single(referencedBy.Children));

        Assert.Equal("Schemas", referencedGroup.Name);
        Assert.Equal("Views", referencingGroup.Name);
        Assert.Equal("CustomerView", Assert.Single(referencingGroup.Children).Name);
    }

    private static TSqlModel CreateModel(string script)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(script);
        return model;
    }

    private sealed class TestTreeItem(TSqlObject source) : ISqlObjectTreeItem
    {
        public string Name => source.Name.Parts.Last();
        public string? IconId => null;
        public string? ToolTip => null;
        public IEnumerable<ITreeItem> Children => [];
        public TSqlObject Source => source;
    }
}
