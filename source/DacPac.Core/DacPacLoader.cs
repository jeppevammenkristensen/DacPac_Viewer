using System.IO.Abstractions;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.Core;

/// <summary>
/// Loads DacPac archives into queryable DacFx models.
/// </summary>
public class DacPacLoader
{
    /// <summary>
    /// Opens a DacPac file and returns its database schema model.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when the DacPac file does not exist.</exception>
    public TSqlModel Load(AbsolutePath source)
    {
        if (!source.FileExists())
        {
            throw new FileNotFoundException($"The specified DacPac file '{source}' does not exist.");
        }

        using var stream = source.OpenRead();

        return TSqlModel.LoadFromDacpac(stream, new ModelLoadOptions()
        {

        });
    }
}
