using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.ViewModels.RecentlyOpened;
using TruePath;
using Xunit;

namespace DacPac.UI.Tests.ViewModels;

public class RecentlyOpenedPageViewModelTest
{
    [Fact]
    public async Task OnActivatedAsync_IdentifiesAvailableAndUnavailableFiles()
    {
        var availablePath = AbsolutePath.Create(@"C:\available.dacpac");
        var unavailablePath = AbsolutePath.Create(@"C:\unavailable.dacpac");
        var settingsService = new SettingsServiceStub([[availablePath, unavailablePath]]);
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [availablePath.Value] = new("dacpac")
        });
        var viewModel = new RecentlyOpenedPageViewModel(settingsService, fileSystem);

        await viewModel.OnActivatedAsync();

        var files = Assert.Single(viewModel.Entries).Files;
        Assert.True(files.Single(file => file.Path == availablePath).IsAvailable);
        Assert.True(files.Single(file => file.Path == unavailablePath).IsUnavailable);
    }

    [Fact]
    public async Task RemoveSelected_RemovesOnlySelectedEntries()
    {
        var firstPath = AbsolutePath.Create(@"C:\first.dacpac");
        var secondPath = AbsolutePath.Create(@"C:\second.dacpac");
        var settingsService = new SettingsServiceStub([[firstPath], [secondPath]]);
        var viewModel = new RecentlyOpenedPageViewModel(settingsService, new MockFileSystem());
        await viewModel.OnActivatedAsync();
        viewModel.Entries.Single(entry => entry.Paths.Single() == firstPath).IsSelected = true;

        viewModel.RemoveSelectedCommand.Execute(null);

        var remainingEntry = Assert.Single(viewModel.Entries);
        Assert.Equal(secondPath, Assert.Single(remainingEntry.Paths));
        Assert.Equal([firstPath], Assert.Single(settingsService.RemovedPaths));
    }

    private sealed class SettingsServiceStub(IEnumerable<AbsolutePath[]> storedPaths) : ISettingsService
    {
        private readonly List<AbsolutePath[]> _storedPaths = [.. storedPaths];

        public List<IReadOnlyList<AbsolutePath>> RemovedPaths { get; } = [];

        public bool EnableBetaUpdates { get; set; }

        public string? LatestConnectionString { get; set; }

        public bool StoreConnectionStrings { get; set; }

        public void SaveOrUpdatePaths(IReadOnlyList<AbsolutePath> paths)
        {
        }

        public IReadOnlyList<AbsolutePath[]> GetStoredPaths() => _storedPaths;

        public void RemovePaths(IReadOnlyList<AbsolutePath> files)
        {
            RemovedPaths.Add(files);
            _storedPaths.RemoveAll(paths => paths.SequenceEqual(files));
        }
    }
}