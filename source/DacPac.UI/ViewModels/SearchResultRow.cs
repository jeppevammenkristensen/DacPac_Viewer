using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels;

/// <summary>
/// A single row displayed in the landing page search results grid.
/// </summary>
public sealed class SearchResultRow(TSqlObject source, string database)
{
    public TSqlObject Source => source;
    public string Database => database;

    public string Name => source.Name.ToString();
    
    public string Type => source.ObjectType.Name;
}
