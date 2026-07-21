using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using DacPac.Core;
using FileBasedApp.Toolkit;
using Microsoft.SqlServer.Dac.Model;
using TruePath;
using Xunit;

namespace DacPac.UI.Tests.Core;

public class DacPacLoaderTest
{
    [Fact]
    public void LoadMultiple_StagesEverySourceBeforeLoadingAndPreservesOriginalPaths()
    {
        var firstSource = AbsolutePath.Create(@"C:\Input\First\database.dacpac");
        var secondSource = AbsolutePath.Create(@"C:\Input\Second\database.dacpac");
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [firstSource.Value] = new("first"),
            [secondSource.Value] = new("second")
        });
        var loadedPaths = new List<AbsolutePath>();
        var loader = new DacPacLoader(
            new TestFileLocations(),
            new NoOpStagingFilesCleanup(),
            fileSystem,
            new FakeTimeProvider(),
            path =>
            {
                Assert.True(fileSystem.File.Exists(path.Value));
                Assert.Equal(2, fileSystem.Directory.GetFiles(TestFileLocations.StagingDirectory.Value, "*.dacpac").Length);
                loadedPaths.Add(path);
                return new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
            });

        var results = loader.LoadMultiple([firstSource, secondSource]).ToList();

        Assert.Equal([firstSource, secondSource], results.Select(result => result.Path));
        Assert.Equal(2, loadedPaths.Distinct().Count());
        Assert.All(loadedPaths, path => Assert.StartsWith(TestFileLocations.StagingDirectory.Value, path.Value));
        Assert.All(results, result => result.Model.Dispose());
    }

    private sealed class TestFileLocations : IFileLocations
    {
        public static readonly AbsolutePath StagingDirectory = Environment.SpecialFolder.LocalApplicationData.GetSpecialFolder() / "DacPacViewerTests" / "DacPacs" / "20260721120000000";

        public AbsolutePath RootSaveLocation => Environment.SpecialFolder.LocalApplicationData.GetSpecialFolder() / "DacPacViewerTests";

        public AbsolutePath TempSaveLocation => RootSaveLocation / "DacPacs";
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class NoOpStagingFilesCleanup : IStagingFilesCleanup
    {
        public void CleanupStagingFiles()
        {
        }
    }
}
