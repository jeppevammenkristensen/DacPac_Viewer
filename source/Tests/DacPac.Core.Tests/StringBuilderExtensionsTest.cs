using System;
using System.Text;
using DacPac.Core.Generators;
using Xunit;

namespace DacPac.Core.Tests;

public class StringBuilderExtensionsTest
{
    [Fact]
    public void SummaryBuilder_WritesSummaryAndReturnsItsBuilder()
    {
        var builder = new StringBuilder();

        var result = new SummaryBuilder("Describes the generated member.", builder)
            .Builder();

        Assert.Same(builder, result);
        Assert.Equal("""
                     /// <summary>
                     /// Describes the generated member. 
                     /// </summary>

                     """.ReplaceLineEndings(Environment.NewLine), result.ToString());
    }

    [Fact]
    public void SummaryBuilder_WithRemarks_WritesEachRemarkLine()
    {
        var result = new SummaryBuilder("Describes the generated member.", new StringBuilder())
            .WithRemarks("First detail.\r\nSecond detail.")
            .Builder()
            .ToString();

        Assert.Equal("""
                     /// <summary>
                     /// Describes the generated member. 
                     /// </summary>
                     /// <remarks>
                     /// First detail.
                     /// Second detail.
                     /// </remarks>

                     """.ReplaceLineEndings(Environment.NewLine), result);
    }

    [Fact]
    public void SummaryBuilder_WithParameter_AppendsParameterDocumentation()
    {
        var result = new SummaryBuilder("Describes the generated member.", new StringBuilder())
            .WithParameter("connection", "The database connection.")
            .Builder()
            .ToString();

        Assert.Equal("""
                     /// <summary>
                     /// Describes the generated member. 
                     /// </summary>
                     ///<param name="connection">The database connection.</param>

                     """.ReplaceLineEndings(Environment.NewLine), result);
    }

    [Fact]
    public void AppendSummary_IncludesRemarksWhenProvided()
    {
        var result = new StringBuilder()
            .AppendSummary("Describes the generated member.", "Additional details.")
            .ToString();

        Assert.Equal("""
                     /// <summary>
                     /// Describes the generated member.
                     /// </summary>
                     /// <remarks>
                     /// Additional details.
                     /// </remarks>

                     """.ReplaceLineEndings(Environment.NewLine), result);
    }

    [Fact]
    public void AppendClassAndProperty_WritesDeclarations()
    {
        var result = new StringBuilder()
            .AppendClass("Customer", "public sealed")
            .AppendProperty("int", "Id")
            .AppendLine("}")
            .ToString();

        Assert.Equal("""
                     public sealed class Customer
                     {
                     public int Id { get; set; }
                     }

                     """.ReplaceLineEndings(Environment.NewLine), result);
    }
}
