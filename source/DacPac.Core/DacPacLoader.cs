using System.IO.Abstractions;
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
    private readonly IStagingFilesCleanup _cleanup;

    private readonly Func<AbsolutePath, TSqlModel> _loadModel;

    public DacPacLoader(IFileLocations location, IStagingFilesCleanup cleanup, IFileSystem fileSystem, TimeProvider timeProvider)
        : this(location, cleanup,fileSystem, timeProvider, LoadModel)
    {
    }

    /// <summary>
    /// Initializes a loader with explicit infrastructure dependencies and a model-loading operation.
    /// </summary>
    /// <remarks>
    /// The model-loading operation is injectable so staging behavior can be tested without requiring DACFx
    /// to read files from the host file system.
    /// </remarks>
    public DacPacLoader(
        IFileLocations location,
        IStagingFilesCleanup cleanup,
        IFileSystem fileSystem,
        TimeProvider timeProvider,
        Func<AbsolutePath, TSqlModel> loadModel)
    {
        _location = location;
        _fileSystem = fileSystem;
        _timeProvider = timeProvider;
        _cleanup = cleanup;
        _loadModel = loadModel;
    }

    /// <summary>
    /// Stages all selected DACPACs, then loads each staged copy while retaining its original path.
    /// </summary>
    public IEnumerable<SqlModelAndPath> LoadMultiple(IReadOnlyList<AbsolutePath> sources)
    {
        _cleanup.CleanupStagingFiles();
        
        var saveLocation = _location.TempSaveLocation / _timeProvider.GetUtcNow().ToString("yyyyMMddHHmmssfff");
        saveLocation.CreateDirectory(_fileSystem);

        var stagedSources = sources
            .Distinct()
            .Select((source, index) => (Source: source, Staged: saveLocation / $"{index:D4}-{source.FileName}"))
            .ToList();

        foreach (var stagedSource in stagedSources)
        {
            stagedSource.Source.FileCopy(stagedSource.Staged, _fileSystem);
        }

        foreach (var stagedSource in stagedSources)
        {
            yield return (_loadModel(stagedSource.Staged), stagedSource.Source);
        }
    }

    private static TSqlModel LoadModel(AbsolutePath path) =>
        TSqlModel.LoadFromDacpac(path.Value, new ModelLoadOptions());
}
