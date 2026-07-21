using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using TruePath;

namespace DacPac.Core;

public interface IStagingFilesCleanup
{
    void CleanupStagingFiles();
}

public partial class StagingFilesCleanup(ILogger<StagingFilesCleanup> logger, IFileLocations fileLocations, IFileSystem fileSystem, TimeProvider timeProvider) : IStagingFilesCleanup
{
    public void CleanupStagingFiles()
    {
        if (!fileLocations.TempSaveLocation.DirectoryExists(fileSystem))
            return;

        var weekAgo = timeProvider.GetUtcNow().AddDays(-1);

        foreach (var oldFolders in fileLocations.TempSaveLocation.EnumerateDirectories(fileSystem)
                     .Where(dir => dir.NewDirectoryInfo(fileSystem).LastAccessTimeUtc < weekAgo))
        { 
            LogDeletingStagingFolderPath(oldFolders);
            oldFolders.DirectoryDelete(true, fileSystem);
        }
    }

    [LoggerMessage(LogLevel.Information, "Deleting staging folder {Path}")]
    partial void LogDeletingStagingFolderPath(AbsolutePath path);
}
