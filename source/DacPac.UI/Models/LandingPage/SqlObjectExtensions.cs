using System.Linq;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.UI.Models.LandingPage;

/// <summary>
/// Provides helpers for reading metadata from DAC model objects.
/// </summary>
public static class SqlObjectExtensions
{
    /// <summary>
    /// Gets the schema referenced by the SQL object, when the object has a schema relationship.
    /// </summary>
    public static ObjectIdentifier? GetSchema(this TSqlObject source)
    {
        if (source.ObjectType.Relationships.FirstOrDefault(relationship => relationship.Name == "Schema") is not { } schemaRelationship)
            return null;

        return source.GetReferenced(schemaRelationship).FirstOrDefault()?.Name;
    }
}
