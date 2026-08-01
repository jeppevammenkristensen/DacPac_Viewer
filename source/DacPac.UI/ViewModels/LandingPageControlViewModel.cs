using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvaloniaEdit.Utils;
using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure.Messages;
using DacPac.UI.ViewModels.Displays;
using DacPac.UI.ViewModels.GeneratedCode;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.UI.ViewModels;

/// <summary>
/// An initial landing page.
/// </summary>
public partial class LandingPageControlViewModel : ScreenPage
{
    private readonly ILogger<LandingPageControlViewModel> _logger;
    private readonly IFilePickerService _filePicker;
    private readonly DacPacLoader _loader;
    private readonly Builder _builder;
    private readonly IClipboardService _clipboard;
    private readonly IServiceLocator _locator;
    private readonly ISettingsService _settingsService;
    private readonly MainWindowViewModel _mainWindow;
    private HashSet<ModelTypeClass> _supportedObjectTypes = [];

    /// <summary>
    /// An initial landing page.
    /// </summary>
    public LandingPageControlViewModel(ILogger<LandingPageControlViewModel> logger,
        IFilePickerService filePicker, 
        DacPacLoader loader,
        Builder builder,
        IClipboardService clipboard,
        IServiceLocator locator,
        ISettingsService settingsService,
        MainWindowViewModel mainWindow)
    {
        _logger = logger;
        _filePicker = filePicker;
        _loader = loader;
        _builder = builder;
        _clipboard = clipboard;
        _locator = locator;
        _settingsService = settingsService;
        _mainWindow = mainWindow;
        
    }

    [NotifyPropertyChangedFor(nameof(Title))] [ObservableProperty]
    private partial string CurrentTitle { get; set; } = "(empty)";

    public override string Title => CurrentTitle;

    [ObservableProperty] public partial bool PreventClose { get; set; }

    /// <summary>The free-text search query.</summary>
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    /// <summary>Options shown in the multi-select filter dropdown. Populated later.</summary>
    [ObservableProperty]
    public partial ObservableCollection<FilterOption> FilterOptions { get; set; } = [];

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


    private bool CanExecuteSelectAll(DataGrid dataGrid)
    {
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteSelectAll))]
    private void SelectAll(DataGrid dataGrid)
    {
        dataGrid.SelectAll();
    }

    /// <summary>Toggles whether a filter option is part of the current selection.</summary>
    [RelayCommand]
    private void ToggleFilter(FilterOption filterOption)
    {
        var filter = filterOption.Type;
        bool isRemoved = SelectedFilters.Remove(filter);
        
        if (filter == "All")
        {
            if (isRemoved)
            {
                SelectedFilters = [];
            }
            else
            {
                SelectedFilters = [.. FilterOptions.Select(option => option.Type)];
            }
        }
        else
        {
            if (!isRemoved)
                SelectedFilters.Add(filter);    
        }

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
    [ObservableProperty] public partial string LoadingMessage { get; set; } = "Loading…";
    [ObservableProperty] public partial IDisplayViewModel Detail { get; set; }

    partial void OnPreventCloseChanged(bool value)
    {
        CanClose = !value;
    }

    private bool CanExecuteInstall()
    {
        return OpenedDacpacFiles.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteInstall))]
    private async Task Install()
    {
        await this.Messenger.Send(new OpenInstallationMessage(OpenedDacpacFiles.Select(AbsolutePath.Create).ToArray()));
        
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
        else if (value.Source.ObjectType == View.TypeClass)
        {
            Detail = new ViewDisplayViewModel(value.Source);
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

    private bool SearchFilter(SearchResultRow row)
    {
        return row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }
    
    private bool CanSearch() => Results.Count > 0;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        FilteredResults =
            [
                ..Results
                    .Where(x => SelectedFilters.Contains(x.Type))
                    .Where(SearchFilter)
            ];
    }

    private bool CanGenerateCode(IList? items) => items is { Count: > 0 };

    /// <summary>Copies the generated code for the selected results to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerateCode))]
    private async Task GenerateCode(IList? items)
    {
        var rows = items?.OfType<SearchResultRow>()
            .Where(x => x.GeneratorSupported)
            .ToArray() ?? [];
        if (rows.Length == 0) return;

        LoadingMessage = "Generating…";
        IsLoading = true;
        try
        {
            var script = await Task.Run(() => _builder.Build([.. rows.Select(x => x.Source)]));

            await _clipboard.SetTextAsync(script);
            var generatedCodePage = _locator.GetRequiredService<GeneratedCodePageViewModel>();
            generatedCodePage.Load(script, rows.Length);
            await _mainWindow.LaunchScreenAsync(generatedCodePage);
            SetStatusMessage(rows.Length == 1
                ? $"Copied generated code for {rows[0].Name} to the clipboard."
                : $"Copied generated code for {rows.Length} objects to the clipboard.");
        }
        finally
        {
            IsLoading = false;
            LoadingMessage = "Loading…";
        }
    }

    [RelayCommand]
    private async Task OpenDacpac()
    {
        var files = await _filePicker.PickDacpacFilesAsync();
        if (files.Count == 0)
            return;

        await OpenDacpacFilesAsync(files.Select(AbsolutePath.Create).ToList());
    }

    /// <summary>
    /// Loads the supplied dacpac files and records them as a recent open operation.
    /// </summary>
    public async Task OpenDacpacFilesAsync(IReadOnlyList<AbsolutePath> files)
    {
        LoadingMessage = "Loading…";
        IsLoading = true;
        try
        {
            if (!CheckFiles(files))
            {
                _settingsService.RemovePaths(files);
                return;
            }
            
            OpenedDacpacFiles.Clear();
            Results.Clear();

            var uniqueFiles = files.Distinct().ToList();
            List<SearchResultRow> searchResultRows = new();
            
            var resultRows = await Task.Run(async () =>
            {
                var source = _loader.LoadMultiple(uniqueFiles).ToList();
                
                _supportedObjectTypes = _builder.GetSupportedObjectTypes();
                
                return 
                    source  
                    .SelectMany(x =>
                        x.Model.GetObjects(DacQueryScopes.UserDefined).Select(y => new {ObjectName = y, x.Path}))
                    .Where(x => x.ObjectName.Name.HasName)
                    .Select(x => new SearchResultRow(x.ObjectName, x.Path.GetFilenameWithoutExtension(),
                        _supportedObjectTypes.Contains(x.ObjectName.ObjectType))).ToList();
            });

            OpenedDacpacFiles.AddRange(uniqueFiles.Select(x => x.Value));
            _settingsService.SaveOrUpdatePaths(uniqueFiles);
            
            searchResultRows.AddRange(resultRows);

            CurrentTitle = string.Join(",", OpenedDacpacFiles.Select(AbsolutePath.Create).Select(x => x.FileName));

            // Computing the filter options touches the DacFx model for every row, so keep it
            // off the UI thread to avoid freezing the window while a dacpac is opened.
            var filterOptions = await Task.Run(() =>
                searchResultRows
                    .GroupBy(row => row.Type)
                    .Select(group => new FilterOption(group.Key, group.Any(row => row.GeneratorSupported)))
                    .OrderBy(option => option.Type)
                    .ToList());

            Results = new ObservableCollection<SearchResultRow>(searchResultRows);
            FilteredResults = [..Results];
            FilterOptions = [new FilterOption("All", false), .. filterOptions];
            SelectedFilters = ["All", .. filterOptions.Select(option => option.Type)];
            SetStatusMessage($"Opened {files.Count} dacpac file(s).");
            InstallCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CheckFiles(IReadOnlyList<AbsolutePath> files)
    {
        var nonExisting = files.Where(x => !x.FileExists()).ToList();
        if (nonExisting.Count > 0)
        {
            Messenger.SendError($"{nonExisting.Count} file(s) was not found at the location");
            return false;
        }

        return true;
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

/// <summary>
/// Represents a selectable object-type filter and whether at least one matching result supports code generation.
/// </summary>
public sealed record FilterOption(string Type, bool CanGenerateCode)
{
    /// <summary>Gets the label shown in the filter dropdown.</summary>
    public string DisplayName => CanGenerateCode ? $"{Type} (can generate code)" : Type;
}
