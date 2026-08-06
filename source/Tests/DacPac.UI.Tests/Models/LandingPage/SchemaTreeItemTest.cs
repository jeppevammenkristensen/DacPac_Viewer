using System.Linq;
using DacPac.UI.Models.LandingPage;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Models.LandingPage;

public class SchemaTreeItemTest
{
    [Fact]
    public void Children_GroupsSupportedObjectsIntoTypeFolders()
    {
        using var model = CreateModel("""
                                      CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);
                                      GO
                                      CREATE VIEW [dbo].[CustomerView] AS SELECT [Id] FROM [dbo].[Customer];
                                      GO
                                      CREATE PROCEDURE [dbo].[GetCustomer] AS SELECT 1;
                                      """);
        var objects = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass, View.TypeClass,
            Procedure.TypeClass).ToList();
        var schema = objects.First(x => x.ObjectType == Table.TypeClass).GetSchema();

        var item = new SchemaTreeItem(Assert.IsType<ObjectIdentifier>(schema), objects);
        var folders = item.Children.Cast<FolderTreeItem>().ToList();

        Assert.Equal(["Tables", "Views", "Procedures"], folders.Select(x => x.Name));
        Assert.IsType<TableTreeItem>(Assert.Single(folders[0].Children));
        Assert.IsType<ViewTreeItem>(Assert.Single(folders[1].Children));
        Assert.IsType<ProcedureTreeItem>(Assert.Single(folders[2].Children));
    }

    private static TSqlModel CreateModel(string script)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(script);
        return model;
    }
}