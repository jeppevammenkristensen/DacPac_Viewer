using System.IO.Abstractions;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.Core;

public class DacPacLoader
{
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
