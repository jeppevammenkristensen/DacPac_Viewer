using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace DacPac.UI.ViewModels;

/// <summary>
/// An initial landing page.
/// </summary>
public partial class LandingPageControlViewModel : ScreenPage
{
    private readonly ILogger<LandingPageControlViewModel> _logger;
    private readonly IFilePickerService _filePicker;

    public LandingPageControlViewModel(
        ILogger<LandingPageControlViewModel> logger,
        IFilePickerService filePicker)
    {
        _logger = logger;
        _filePicker = filePicker;
    }

    public override string Title => "Landing page";

    [ObservableProperty] public partial bool PreventClose { get; set; }

    /// <summary>The free-text search query.</summary>
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    /// <summary>Options shown in the multi-select filter dropdown. Populated later.</summary>
    public ObservableCollection<string> FilterOptions { get; } = [];

    /// <summary>The currently selected filter options (bound to the ListBox selection).</summary>
    public ObservableCollection<string> SelectedFilters { get; } = [];

    /// <summary>Rows shown in the results grid. Populated later.</summary>
    public ObservableCollection<SearchResultRow> Results { get; } = [];

    /// <summary>The currently selected result row.</summary>
    [ObservableProperty] public partial SearchResultRow? SelectedResult { get; set; }

    /// <summary>Detail text shown in the read-only panel for the selected result.</summary>
    [ObservableProperty] public partial string DetailsText { get; set; } = string.Empty;

    /// <summary>Paths of dacpac files chosen via File ▸ Open dacpac.</summary>
    public ObservableCollection<string> OpenedDacpacFiles { get; } = [];

    partial void OnPreventCloseChanged(bool value)
    {
        CanClose = !value;
    }

    partial void OnSelectedResultChanged(SearchResultRow? value)
    {
        // TODO: refresh DetailsText from the selected result.
    }

    [RelayCommand]
    private void Search()
    {
        // TODO: populate Results / DetailsText based on SearchText and SelectedFilters.
    }

    [RelayCommand]
    private async Task OpenDacpac()
    {
        var files = await _filePicker.PickDacpacFilesAsync();
        if (files.Count == 0)
            return;

        OpenedDacpacFiles.Clear();
        foreach (var file in files)
            OpenedDacpacFiles.Add(file);

        SetStatusMessage($"Opened {files.Count} dacpac file(s).");

        // TODO: load/parse selected dacpac files and populate search data.
    }

    public override Task OnActivatedAsync()
    {
        _logger.LogInformation("On Activated");
        return Task.CompletedTask;
    }

    public override Task CloseAsync()
    {
        _logger.LogInformation("On Close");
        return Task.CompletedTask;
    }
}
