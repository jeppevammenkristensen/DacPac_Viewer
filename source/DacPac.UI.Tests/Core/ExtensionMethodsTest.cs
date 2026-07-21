using System.Linq;
using DacPac.Core;
using Microsoft.SqlServer.Dac.Model;
using Xunit;

namespace DacPac.UI.Tests.Core;

public class ExtensionMethodsTest
{
    [Fact]
    public void GetDotNetDataType_MapsUserDefinedTypeName()
    {
        using var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        model.AddObjects("""
                         CREATE TYPE [dbo].[PhoneNumber] FROM nvarchar(20);
                         CREATE TABLE [dbo].[Customer] ([Phone] [dbo].[PhoneNumber] NULL);
                         """);
        var column = model.GetObjects(DacQueryScopes.UserDefined, Column.TypeClass).Single();
        var dataType = column.GetReferenced(Column.DataType).Single();

        var result = dataType.GetDotNetDataType(nullable: true);

        Assert.Equal(new DotnetType("PhoneNumber", true), result);
    }
}
