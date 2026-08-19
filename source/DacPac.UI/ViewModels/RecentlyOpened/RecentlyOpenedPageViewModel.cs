using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure;

namespace DacPac.UI.ViewModels.RecentlyOpened;

/// <summary>
/// Displays remembered DacPac open operations and removes selected entries.
/// </summary>
public partial class RecentlyOpenedPageViewModel : ScreenPage
{
    private readonly ISettingsService _settingsService;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the recently opened page.
    /// </summary>
    public RecentlyOpenedPageViewModel(ISettingsService settingsService, IFileSystem fileSystem)
    {
        _settingsService = settingsService;
        _fileSystem = fileSystem;
        Entries.CollectionChanged += EntriesCollectionChanged;
    }

    /// <inheritdoc />
    public override string Title => "Recently Opened";

    /// <summary>
    /// Gets the remembered DacPac open operations.
    /// </summary>
    public ObservableCollection<RecentDacpacEntry> Entries { get; } = [];

    /// <summary>
    /// Gets whether one or more entries are selected for removal.
    /// </summary>
    public bool HasSelectedEntries => Entries.Any(entry => entry.IsSelected);

    /// <summary>
    /// Gets whether there are no remembered open operations to display.
    /// </summary>
    public bool HasNoEntries => Entries.Count == 0;

    /// <inheritdoc />
    public override Task OnActivatedAsync()
    {
        RefreshEntries();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the selected remembered open operations.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedEntries))]
    private void RemoveSelected()
    {
        foreach (var entry in Entries.Where(entry => entry.IsSelected).ToArray())
            _settingsService.RemovePaths(entry.Paths);

        RefreshEntries();
    }

    /// <summary>
    /// Reloads entries from persisted settings and determines their file availability.
    /// </summary>
    private void RefreshEntries()
    {
        foreach (var entry in Entries)
            entry.PropertyChanged -= EntryPropertyChanged;

        Entries.Clear();
        foreach (var paths in _settingsService.GetStoredPaths())
        {
            var entry = new RecentDacpacEntry(paths, _fileSystem);
            entry.PropertyChanged += EntryPropertyChanged;
            Entries.Add(entry);
        }

        RemoveSelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Refreshes command availability when an entry selection changes.
    /// </summary>
    private void EntryPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName == nameof(RecentDacpacEntry.IsSelected))
            RemoveSelectedCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Updates the empty-state display when the entry collection changes.
    /// </summary>
    private void EntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(HasNoEntries));
    }
}
