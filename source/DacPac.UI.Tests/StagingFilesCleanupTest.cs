using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using DacPac.Core;
using FileBasedApp.Toolkit;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using TruePath;
using Xunit;

namespace DacPac.UI.Tests;

[TestSubject(typeof(StagingFilesCleanup))]
public class StagingFilesCleanupTest
{
    [Fact]
    public void CleanupStagingFiles_DeletesStagingFoldersOlderThanOneDay()
    {
        var oldStagingFolder = TestFileLocations.TempSaveLocation / "old";
        var recentStagingFolder = TestFileLocations.TempSaveLocation / "recent";
        var fileSystem = new MockFileSystem();
        oldStagingFolder.CreateDirectory(fileSystem);
        recentStagingFolder.CreateDirectory(fileSystem);
        oldStagingFolder.NewDirectoryInfo(fileSystem).LastWriteTimeUtc = FakeTimeProvider.Now.AddHours(-25).UtcDateTime;
        recentStagingFolder.NewDirectoryInfo(fileSystem).LastWriteTimeUtc = FakeTimeProvider.Now.AddHours(-23).UtcDateTime;

        CreateCleanup(fileSystem).CleanupStagingFiles();

        Assert.False(oldStagingFolder.DirectoryExists(fileSystem));
        Assert.True(recentStagingFolder.DirectoryExists(fileSystem));
    }

    [Fact]
    public void CleanupStagingFiles_PreservesOldFoldersOutsideTheStagingDirectory()
    {
        var unrelatedFolder = TestFileLocations.RootSaveLocation / "settings";
        var fileSystem = new MockFileSystem();
        unrelatedFolder.CreateDirectory(fileSystem);
        unrelatedFolder.NewDirectoryInfo(fileSystem).LastWriteTimeUtc = FakeTimeProvider.Now.AddHours(-25).UtcDateTime;

        CreateCleanup(fileSystem).CleanupStagingFiles();

        Assert.True(unrelatedFolder.DirectoryExists(fileSystem));
    }

    private static StagingFilesCleanup CreateCleanup(MockFileSystem fileSystem) =>
        new(NullLogger<StagingFilesCleanup>.Instance, new TestFileLocations(), fileSystem, new FakeTimeProvider());

    private sealed class FakeTimeProvider : TimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class TestFileLocations : IFileLocations
    {
        public static readonly AbsolutePath RootSaveLocation = Environment.SpecialFolder.LocalApplicationData.GetSpecialFolder() / "DacPacViewerTests";
        public static readonly AbsolutePath TempSaveLocation = RootSaveLocation / "TempDacPacs";

        AbsolutePath IFileLocations.RootSaveLocation => RootSaveLocation;

        AbsolutePath IFileLocations.TempSaveLocation => TempSaveLocation;
    }
}
