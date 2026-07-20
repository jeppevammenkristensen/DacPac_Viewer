using System.IO.Abstractions;
using DacPac.UI.ApplicationLayer;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.Core;

using SqlModelAndPath = (TSqlModel Model, AbsolutePath Path);

/// <summary>
/// Loads DacPac archives into queryable DacFx models.
/// </summary>
public class DacPacLoader
{
    private readonly IFileLocations _location;
    private readonly IFileSystem _fileSystem;
    private readonly TimeProvider _timeProvider;

    public DacPacLoader(IFileLocations location) : this(location, new FileSystem(), TimeProvider.System)
    {
    }

    public DacPacLoader(IFileLocations location, IFileSystem fileSystem, TimeProvider timeProvider)
    {
        _location = location;
        _fileSystem = fileSystem;
        _timeProvider = timeProvider;
    }
   
    // Removed. Use LoadMultiple
    // /// <summary>
    // /// Opens a DacPac file and returns its database schema model.
    // /// </summary>
    // /// <exception cref="FileNotFoundException">Thrown when the DacPac file does not exist.</exception>
    // public TSqlModel Load(AbsolutePath source)
    // {
    //     if (!source.FileExists())
    //     {
    //         throw new FileNotFoundException($"The specified DacPac file '{source}' does not exist.");
    //     }
    //
    //     using var stream = source.OpenRead();
    //
    //     return TSqlModel.LoadFromDacpac(stream, new ModelLoadOptions()
    //     {
    //
    //     });
    // }
    
    public IEnumerable<SqlModelAndPath> LoadMultiple(IReadOnlyList<AbsolutePath> sources)
    {
        var saveLocation = _location.TempSaveLocation / _timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff");
        saveLocation.CreateDirectory();

        foreach (var absolutePath in sources)
        {
            absolutePath.FileCopy(saveLocation / absolutePath.FileName);
        }
        
        foreach (var file in saveLocation.EnumerateFiles("*.dacpac"))
        {
            yield return (TSqlModel.LoadFromDacpac(file.Value, new ModelLoadOptions() { }), file);
        }
        
    }
}
