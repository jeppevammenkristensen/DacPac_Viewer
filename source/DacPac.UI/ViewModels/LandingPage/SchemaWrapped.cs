using DacPac.Wrappers;

namespace DacPac.UI.ViewModels.LandingPage;

/// <summary>
/// Adapts a schema model as a selectable schema option.
/// </summary>
public class SchemaWrapped : ISchemaOption
{
    public readonly SchemaWrapper Wrapped;

    public SchemaWrapped(SchemaWrapper wrapped)
    {
        Wrapped = wrapped;
    }

    /// <summary>
    /// Gets the schema display name.
    /// </summary>
    public string Display => Wrapped.SqlObject.Name.ToString();
}
