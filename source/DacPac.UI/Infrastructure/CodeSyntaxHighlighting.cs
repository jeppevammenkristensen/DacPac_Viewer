using System;
using System.Xml;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace DacPac.UI.Infrastructure;

internal static class CodeSyntaxHighlighting
{
    private static readonly Uri GeneratedCSharpDefinitionUri = new("avares://DacPac.UI/Assets/Highlighting/GeneratedCSharp.xshd");
    private static readonly Lazy<IHighlightingDefinition> GeneratedCSharpDefinition = new(LoadGeneratedCSharpDefinition);

    /// <summary>Gets highlighting appropriate for SQL scripts or generated C# code.</summary>
    public static IHighlightingDefinition For(string text)
    {
        return text.TrimStart().StartsWith("public ", StringComparison.Ordinal)
            ? GeneratedCSharpDefinition.Value
            : HighlightingManager.Instance.GetDefinition("TSQL");
    }

    private static IHighlightingDefinition LoadGeneratedCSharpDefinition()
    {
        using var stream = AssetLoader.Open(GeneratedCSharpDefinitionUri);
        using var reader = XmlReader.Create(stream);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
