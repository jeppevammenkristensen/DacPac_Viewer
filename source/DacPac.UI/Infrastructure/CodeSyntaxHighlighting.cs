using System;
using System.Xml;
using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace DacPac.UI.Infrastructure;

internal static class CodeSyntaxHighlighting
{
    private static readonly Uri GeneratedCSharpDefinitionUri = new("avares://DacPac.UI/Assets/Highlighting/GeneratedCSharp.xshd");
    private static readonly Uri SqlDefinitionUri = new("avares://DacPac.UI/Assets/Highlighting/Sql.xshd");
    private static readonly Lazy<IHighlightingDefinition> GeneratedCSharpDefinition = new(LoadGeneratedCSharpDefinition);
    private static readonly Lazy<IHighlightingDefinition> SqlDefinition = new(LoadSqlDefinition);

    /// <summary>Gets C# syntax highlighting appropriate for the current theme.</summary>
    /// <remarks>In dark mode a Variant is used that has better contrast. In light mode the default is used</remarks>
    public static IHighlightingDefinition CSharp => IsDarkTheme
        ? GeneratedCSharpDefinition.Value
        : HighlightingManager.Instance.GetDefinition("C#");

    /// <summary>Gets SQL syntax highlighting appropriate for the current theme.</summary>
    public static IHighlightingDefinition Sql => IsDarkTheme
        ? SqlDefinition.Value
        : HighlightingManager.Instance.GetDefinition("TSQL");

    private static bool IsDarkTheme => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

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
