using System;
using System.Xml;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace DacPac.UI.Infrastructure;

internal static class CodeSyntaxHighlighting
{
    private static readonly Uri GeneratedCSharpDefinitionUri = new("avares://DacPac.UI/Assets/Highlighting/GeneratedCSharp.xshd");
    private static readonly Uri SqlDefinitionUri = new("avares://DacPac.UI/Assets/Highlighting/Sql.xshd");
    private static readonly Lazy<IHighlightingDefinition> GeneratedCSharpDefinition = new(LoadGeneratedCSharpDefinition);
    private static readonly Lazy<IHighlightingDefinition> SqlDefinition = new(LoadSqlDefinition);

    /// <summary>Gets the high-contrast syntax definition for generated C# code.</summary>
    public static IHighlightingDefinition GeneratedCSharp => GeneratedCSharpDefinition.Value;

    /// <summary>Gets highlighting appropriate for SQL scripts or generated C# code.</summary>
    public static IHighlightingDefinition For(string text)
    {
        return text.TrimStart().StartsWith("public ", StringComparison.Ordinal)
            ? GeneratedCSharpDefinition.Value
            : SqlDefinition.Value;
    }

    private static IHighlightingDefinition LoadGeneratedCSharpDefinition()
        => LoadDefinition(GeneratedCSharpDefinitionUri);

    private static IHighlightingDefinition LoadSqlDefinition()
        => LoadDefinition(SqlDefinitionUri);

    private static IHighlightingDefinition LoadDefinition(Uri definitionUri)
    {
        using var stream = AssetLoader.Open(definitionUri);
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
