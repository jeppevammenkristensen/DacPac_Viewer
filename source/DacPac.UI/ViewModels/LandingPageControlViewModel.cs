using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.Core;
using DacPac.UI.ViewModels.Displays;
using DacPac.UI.ViewModels.GeneratedCode;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.UI.ViewModels;

/// <summary>
/// An initial landing page.
/// </summary>
public partial class LandingPageControlViewModel(
    ILogger<LandingPageControlViewModel> logger,
    IFilePickerService filePicker, 
    DacPacLoader loader,
    Builder builder,
    IClipboardService clipboard,
    IServiceLocator locator,
    MainWindowViewModel mainWindow)
    : ScreenPage
{

    [NotifyPropertyChangedFor(nameof(Title))] [ObservableProperty]
    private partial string CurrentTitle { get; set; } = "(empty)";

    public override string Title => CurrentTitle;

    [ObservableProperty] public partial bool PreventClose { get; set; }

    /// <summary>The free-text search query.</summary>
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    /// <summary>Options shown in the multi-select filter dropdown. Populated later.</summary>
    [ObservableProperty]
    public partial ObservableCollection<string> FilterOptions { get; set; } = [];

    /// <summary>The currently selected filter options (bound to the ListBox selection).</summary>
    /// <summary>The currently selected filter options (bound to the combobox checkboxes).</summary>
    [ObservableProperty]
    public partial ObservableCollection<string> SelectedFilters { get; set; } = [];

    /// <summary>Summary shown in the collapsed filter combobox.</summary>
    public string FilterSummary => SelectedFilters.Count == 0 ? "Filters" : $"{SelectedFilters.Count} selected";

    partial void OnSelectedFiltersChanged(ObservableCollection<string> value)
    {
        OnPropertyChanged(nameof(FilterSummary));
    }

    /// <summary>Toggles whether a filter option is part of the current selection.</summary>
    [RelayCommand]
    private void ToggleFilter(string filter)
    {
        if (!SelectedFilters.Remove(filter))
            SelectedFilters.Add(filter);

        OnPropertyChanged(nameof(SelectedFilters));
        OnPropertyChanged(nameof(FilterSummary));

        if (SearchCommand.CanExecute(null))
            SearchCommand.Execute(null);
        
    }

    /// <summary>Rows shown in the results grid. Populated later.</summary>
    /// <summary>Rows shown in the results grid. Populated later.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    public partial ObservableCollection<SearchResultRow> Results { get; set; } = [];
    
    [ObservableProperty] 
    public partial ObservableCollection<SearchResultRow> FilteredResults { get; set; } = [];

    /// <summary>The currently selected result row.</summary>
    [NotifyCanExecuteChangedFor(nameof(GenerateCodeCommand))]
    [ObservableProperty]
    public partial SearchResultRow? SelectedResult { get; set; }

    /// <summary>Detail text shown in the read-only panel for the selected result.</summary>
    [ObservableProperty] public partial string DetailsText { get; set; } = string.Empty;

    /// <summary>Paths of dacpac files chosen via File ▸ Open dacpac.</summary>
    public ObservableCollection<string> OpenedDacpacFiles { get; } = [];

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial IDisplayViewModel Detail { get; set; }

    partial void OnPreventCloseChanged(bool value)
    {
        CanClose = !value;
    }

    partial void OnSelectedResultChanged(SearchResultRow? value)
    {
        if (value is null) return;
        if (value.Source.ObjectType == Table.TypeClass)
        {
            Detail = new TableDisplayViewModel(value.Source);
        }
        else if (value.Source.ObjectType == Procedure.TypeClass)
           
        {
            Detail = new ProcedureDisplayViewModel(value.Source);
        }
        else
        {
            Detail = new DefaultDisplayViewModel(value.Source);    
        }
        
        
        //
        //
        // // TODO: refresh DetailsText from the selected result
        // DetailsText = value?.Source.GetScript() ?? "Not available";
        
    }

    private bool SearcFilter(SearchResultRow row)
    {
        return row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }
    
    private bool CanSearch() => Results.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        // TODO: populate Results / DetailsText based on SearchText and SelectedFilters.
        if (SelectedFilters.Any(x => x == "All"))
        {
            FilteredResults = [..Results.Where(SearcFilter)];
        }
        else
        {
            FilteredResults =
            [
                ..Results
                    .Where(x => SelectedFilters.Contains(x.Type))
                    .Where(SearcFilter)
            ];
        }
    }

    private bool CanGenerateCode(IList? items) => items is { Count: > 0 };

    /// <summary>Copies the generated code for the selected results to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerateCode))]
    private async Task GenerateCode(IList? items)
    {
        var rows = items?.OfType<SearchResultRow>().ToArray() ?? [];
        if (rows.Length == 0) return;

        var script = builder.Build(rows.Select(x => x.Source).ToArray());
        await clipboard.SetTextAsync(script);
        var generatedCodePage = locator.GetRequiredService<GeneratedCodePageViewModel>();
        generatedCodePage.Load(script, rows.Length);
        await mainWindow.LaunchScreenAsync(generatedCodePage);
        SetStatusMessage(rows.Length == 1
            ? $"Copied generated code for {rows[0].Name} to the clipboard."
            : $"Copied generated code for {rows.Length} objects to the clipboard.");
    }

    [RelayCommand]
    private async Task OpenDacpac()
    {
        var files = await filePicker.PickDacpacFilesAsync();
        if (files.Count == 0)
            return;

        IsLoading = true;
        try
        {
            OpenedDacpacFiles.Clear();
            Results.Clear();

            var uniqueFiles = files.Select(AbsolutePath.Create).ToList();
            List<SearchResultRow> searchResultRows = new();
            var resultRows = await Task.Run(() =>loader.LoadMultiple(uniqueFiles)
                .SelectMany(x => x.Model.GetObjects(DacQueryScopes.UserDefined).Select(y => new {ObjectName = y, x.Path}))
                .Where(x => x.ObjectName.Name.HasName)
                .Select(x => new SearchResultRow(x.ObjectName, x.Path.GetFilenameWithoutExtension())).ToList());

            OpenedDacpacFiles.AddRange(uniqueFiles.Select(x => x.Value));
            
            searchResultRows.AddRange(resultRows);

            CurrentTitle = string.Join(",", OpenedDacpacFiles.Select(AbsolutePath.Create).Select(x => x.FileName));

            // Computing the filter options touches the DacFx model for every row, so keep it
            // off the UI thread to avoid freezing the window while a dacpac is opened.
            var filterOptions = await Task.Run(() =>
                searchResultRows.Select(x => x.Type).Distinct().Order().ToList());

            Results = new ObservableCollection<SearchResultRow>(searchResultRows);
            FilteredResults = [..Results];
            FilterOptions = ["All", ..filterOptions];
            SelectedFilters = [FilterOptions[0]];
            SetStatusMessage($"Opened {files.Count} dacpac file(s).");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public override Task OnActivatedAsync()
    {
        logger.LogInformation("On Activated");
        return Task.CompletedTask;
    }

    public override Task CloseAsync()
    {
        logger.LogInformation("On Close");
        return Task.CompletedTask;
    }
}
