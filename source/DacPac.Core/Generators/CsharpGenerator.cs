using System.Text;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core.Generators;

/// <summary>
/// Provides naming helpers for C# source generators.
/// </summary>
public static class GeneratorExtensions
{
    /// <summary>
    /// Creates a PascalCase generated type name for a DacPac object.
    /// </summary>
    public static string GenerateTypeName(this TSqlObject sqlObject, string postfix)
    {
        var parts = sqlObject.Name.Parts.Where(x => !string.Equals(x, "dbo", StringComparison.OrdinalIgnoreCase));
        return $"{string.Join("_", parts.Select(x => x.ToPascalCase()))}_{postfix}";
    }
}

/// <summary>
/// Emits a comment when no generator supports a DacPac object.
/// </summary>
public class NotFoundGenerator : CsharpGenerator
{
    /// <summary>
    /// Gets an empty type name because this generator emits no type.
    /// </summary>
    public override string TypeName(TSqlObject sqlObject)
    {
        return string.Empty;
    }

    /// <summary>
    /// Gets the supported types, which are unrestricted for this fallback generator.
    /// </summary>
    public override ModelTypeClass[] SupportedObjectTypes => [];

    protected override void DoBuild(TSqlObject sqlObject, StringBuilder sb)
    {
        sb.AppendLine($"// No generator found for {sqlObject.Name} of type {sqlObject.ObjectType}");
    }

    /// <summary>
    /// Determines whether the supplied object can be handled by this fallback generator.
    /// </summary>
    public override bool IsValid(TSqlObject tSqlObject)
    {
        return true;
    }
}

/// <summary>
/// Defines the common validation and output flow for generators that convert DacPac objects into C# source.
/// </summary>
public abstract class CsharpGenerator
{
    /// <summary>
    /// Gets the generated type name for the supplied DacPac object.
    /// </summary>
    public abstract string TypeName(TSqlObject sqlObject);
    
    /// <summary>
    /// Gets the DacPac object types supported by this generator.
    /// </summary>
    public abstract ModelTypeClass[] SupportedObjectTypes { get; } 
    
    /// <summary>
    /// Gets additional DacPac objects required to generate the supplied object.
    /// </summary>
    public virtual IEnumerable<TSqlObject> RequiredObjects(TSqlObject sqlObject) => Enumerable.Empty<TSqlObject>();
    
    /// <summary>
    /// Appends generated C# source for a supported DacPac object.
    /// </summary>
    public StringBuilder Build(TSqlObject tSqlObject, StringBuilder? sb = null)
    {
        if (!IsValid(tSqlObject))
        {
            throw new InvalidOperationException($"The provided TSqlObject '{tSqlObject.Name}' is not valid for this generator.");
        }
        
        sb ??= new StringBuilder();
        DoBuild(tSqlObject, sb);
        return sb;
    }

    /// <summary>
    /// Writes generator-specific C# source after the object has been validated.
    /// </summary>
    protected abstract void DoBuild(TSqlObject sqlObject, StringBuilder sb);

    /// <summary>
    /// Determines whether this generator supports the supplied DacPac object.
    /// </summary>
    public virtual bool IsValid(TSqlObject tSqlObject) => SupportedObjectTypes.Any(x => tSqlObject.ObjectType == x);

}
