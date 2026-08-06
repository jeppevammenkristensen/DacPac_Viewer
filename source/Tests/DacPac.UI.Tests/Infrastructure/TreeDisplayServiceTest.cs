using System.Linq;
using DacPac.UI.Infrastructure;
using DacPac.UI.Models.LandingPage;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Infrastructure;

public class TreeDisplayServiceTest
{
    [Fact]
    public void GetRoots_GroupsSupportedObjectsBySchemaInNameOrder()
    {
        using var model = CreateModel("""
                                      CREATE SCHEMA [alpha];
                                      GO
                                      CREATE TABLE [dbo].[Customer] ([Id] int NOT NULL);
                                      GO
                                      CREATE VIEW [dbo].[CustomerView] AS SELECT [Id] FROM [dbo].[Customer];
                                      GO
                                      CREATE PROCEDURE [alpha].[GetCustomer] AS SELECT 1;
                                      """);

        var roots = new TreeDisplayService().GetRoots([model]).Cast<SchemaTreeItem>().ToList();

        Assert.Equal(["alpha", "dbo"], roots.Select(x => x.Name));
        Assert.Equal(["Procedures"], roots[0].Children.Select(x => x.Name));
        Assert.Equal(["Tables", "Views"], roots[1].Children.Select(x => x.Name));
    }

    [Fact]
    public void GetRoots_ExcludesUnsupportedObjectTypes()
    {
        using var model = CreateModel("CREATE TYPE [dbo].[CustomerId] FROM int;");

        var roots = new TreeDisplayService().GetRoots([model]).ToList();

        Assert.Empty(roots);
    }

    private static TSqlModel CreateModel(string script)
    {
        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects(script);
        return model;
    }
}