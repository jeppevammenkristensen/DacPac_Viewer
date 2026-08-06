using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.ViewModels.LandingPage;

/// <summary>
/// A single row displayed in the landing page search results grid.
/// </summary>
public sealed partial class SearchResultRow : ObservableObject
{
    public SearchResultRow(TSqlObject source, string database, bool generatorSupported, ObjectIdentifier? schema)
    {
        Source = source;
        Database = database;
        GeneratorSupported = generatorSupported;
        Schema = schema;
    }

    [ObservableProperty]
    public partial bool GeneratorSupported { get; set; }
    public TSqlObject Source { get; }
    public string Database { get; }

    public string Name => Source.Name.ToString();
    
    public string Type => Source.ObjectType.Name;

    public ObjectIdentifier? Schema { get; }
    
    public void TriggerGeneratorSupportedChanged() => OnPropertyChanged(nameof(GeneratorSupported));
}
