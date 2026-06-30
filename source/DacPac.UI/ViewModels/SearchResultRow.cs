namespace DacPac.UI.ViewModels;

/// <summary>
/// A single row displayed in the landing page search results grid.
/// </summary>
public sealed class SearchResultRow
{
    public string Database { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}
