using DacPac.Wrappers;

namespace DacPac.UI.ViewModels.LandingPage;

public class SchemaWrapped : ISchemaOption
{
    public readonly SchemaWrapper Wrapped;

    public SchemaWrapped(SchemaWrapper wrapped)
    {
        Wrapped = wrapped;
    }

    public string Display => Wrapped.SqlObject.Name.ToString();
}