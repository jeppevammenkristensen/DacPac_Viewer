using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.LandingPage;

/// <summary>
/// A single row displayed in the landing page search results grid.
/// </summary>
public sealed class SearchResultRow(TSqlObject source, string database, bool generatorSupported)
{
    public bool GeneratorSupported { get; } = generatorSupported;
    public TSqlObject Source => source;
    public string Database => database;

    public string Name => source.Name.ToString();
    
    public string Type => source.ObjectType.Name;

    public ObjectIdentifier? Schema
    {
        get
        {
            if (source.ObjectType.Relationships.FirstOrDefault(x => x.Name == "Schema") is { } hasSchema)
            {
                return source.GetReferenced(hasSchema).First().Name;
            }

            return null;
        }
    }
    
    
}
