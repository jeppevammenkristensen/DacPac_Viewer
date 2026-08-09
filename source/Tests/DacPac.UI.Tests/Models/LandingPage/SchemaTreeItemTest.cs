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
                                      GO
                                      CREATE TYPE [dbo].[CustomerType] AS TABLE ([Id] int NOT NULL);
                                      """);
        var objects = model.GetObjects(DacQueryScopes.UserDefined, Table.TypeClass, View.TypeClass,
            Procedure.TypeClass, TableType.TypeClass).ToList();
        var schema = objects.First(x => x.ObjectType == Table.TypeClass).GetSchema();

        var item = new SchemaTreeItem(Assert.IsType<ObjectIdentifier>(schema), objects);
        var folders = item.Children.Cast<FolderTreeItem>().ToList();

        Assert.Equal(["Tables", "Views", "Procedures", "Table Types"], folders.Select(x => x.Name));
        Assert.IsType<TableTreeItem>(Assert.Single(folders[0].Children));
        Assert.IsType<ViewTreeItem>(Assert.Single(folders[1].Children));
        Assert.IsType<ProcedureTreeItem>(Assert.Single(folders[2].Children));
        Assert.IsType<TableTypeTreeItem>(Assert.Single(folders[3].Children));
    }

    [Fact]
    public void TableTypeChildren_GroupsProcedureParametersUnderTheirProcedure()
    {
        using var model = CreateModel("""
                                      CREATE TYPE [dbo].[CustomerType] AS TABLE ([Id] int NOT NULL);
                                      GO
                                      CREATE PROCEDURE [dbo].[GetCustomers] @Customers [dbo].[CustomerType] READONLY,
                                          @MoreCustomers [dbo].[CustomerType] READONLY AS SELECT 1;
                                      """);
        var tableType = Assert.Single(model.GetObjects(DacQueryScopes.UserDefined, TableType.TypeClass));

        var item = new TableTypeTreeItem(tableType);
        var referencedBy = Assert.IsType<FolderTreeItem>(Assert.Single(item.Children, x => x.Name == "Referenced by"));
        var procedures = Assert.IsType<TypeGroupTreeItem>(Assert.Single(referencedBy.Children));
        var procedure = Assert.IsAssignableFrom<ISqlObjectTreeItem>(Assert.Single(procedures.Children));
        var parameters = procedure.Children.Cast<ISqlObjectTreeItem>().ToList();

        Assert.Equal("GetCustomers", procedure.Name);
        Assert.Equal(Procedure.TypeClass, procedure.Source.ObjectType);
        Assert.Equal(["@Customers", "@MoreCustomers"], parameters.Select(x => x.Name));
        Assert.All(parameters, parameter => Assert.Equal(Parameter.TypeClass, parameter.Source.ObjectType));
    }

    private static TSqlModel CreateModel(string script)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(script);
        return model;
    }
}
