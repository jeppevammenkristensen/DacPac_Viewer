using System.Text;
using Microsoft.SqlServer.Dac.Model;

namespace DacPac.Core;

/// <summary>
/// Defines the common validation and output flow for generators that convert DacPac objects into C# source.
/// </summary>
public abstract class CsharpGenerator
{
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
    public abstract bool IsValid(TSqlObject tSqlObject);

}
