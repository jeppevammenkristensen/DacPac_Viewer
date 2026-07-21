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

        var retentionCutoff = timeProvider.GetUtcNow().AddDays(-1);

        foreach (var oldFolders in fileLocations.TempSaveLocation.EnumerateDirectories(fileSystem)
                     .Where(dir => dir.NewDirectoryInfo(fileSystem).LastWriteTimeUtc < retentionCutoff))
        {
            try
            {
                LogDeletingStagingFolderPath(oldFolders);
                oldFolders.DirectoryDelete(true, fileSystem);
            }
            catch (IOException exception)
            {
                LogFailedToDeleteStagingFolder(exception, oldFolders);
            }
            catch (UnauthorizedAccessException exception)
            {
                LogFailedToDeleteStagingFolder(exception, oldFolders);
            }
        }
    }

    [LoggerMessage(LogLevel.Information, "Deleting staging folder {Path}")]
    partial void LogDeletingStagingFolderPath(AbsolutePath path);

    [LoggerMessage(LogLevel.Warning, "Failed to delete stale staging folder {Path}")]
    partial void LogFailedToDeleteStagingFolder(Exception exception, AbsolutePath path);
}
