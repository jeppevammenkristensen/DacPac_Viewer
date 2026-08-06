using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Utils;
using DacPac.UI.Infrastructure;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Input;
using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;
using DacPac.UI.Infrastructure.Messages;
using DacPac.UI.Models.LandingPage;
using DacPac.UI.ViewModels.Displays;
using DacPac.UI.ViewModels.GeneratedCode;
using DacPac.Wrappers;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Dac.Model;
using TruePath;

namespace DacPac.UI.ViewModels.LandingPage;

/// <summary>
/// An initial landing page.
/// </summary>
public partial class LandingPageControlViewModel : ScreenPage, IRecipient<ThemeChangedMessage>
{
    private readonly ILogger<LandingPageControlViewModel> _logger;
    private readonly IFilePickerService _filePicker;
    private readonly DacPacLoader _loader;
    private readonly Builder _builder;
    private readonly IClipboardService _clipboard;
    private readonly IServiceLocator _locator;
    private readonly ISettingsService _settingsService;
    private readonly TreeDisplayService _treeDisplayService;
    private readonly MainWindowViewModel _mainWindow;
    private static readonly ObjectIdentifierComparer ObjectIdentifierComparer = new();
    private HashSet<ModelTypeClass> _supportedObjectTypes = [];

    /// <summary>
    /// Initializes the landing page and its application services.
    /// </summary>
    public LandingPageControlViewModel(ILogger<LandingPageControlViewModel> logger,
        IFilePickerService filePicker,
        DacPacLoader loader,
        Builder builder,
        IClipboardService clipboard,
        IServiceLocator locator,
        ISettingsService settingsService,
        TreeDisplayService treeDisplayService,
        MainWindowViewModel mainWindow)
    {
        _logger = logger;
        _filePicker = filePicker;
        _loader = loader;
        _builder = builder;
        _clipboard = clipboard;
        _locator = locator;
        _settingsService = settingsService;
        _treeDisplayService = treeDisplayService;
        _mainWindow = mainWindow;
        SelectedSchemaFilters = [];
    }

    /// <summary>
    /// Gets or sets the title derived from the currently opened dacpac files.
    /// </summary>
    [NotifyPropertyChangedFor(nameof(Title))]
    [ObservableProperty]
    private partial string CurrentTitle { get; set; } = "(empty)";

    /// <summary>
    /// Gets the title displayed for this page.
    /// </summary>
    public override string Title => CurrentTitle;

    /// <summary>
    /// Gets or sets whether the page must remain open while an operation is in progress.
    /// </summary>
    [ObservableProperty]
    public partial bool PreventClose { get; set; }

    /// <summary>The free-text search query.</summary>
    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    /// <summary>Options shown in the multi-select filter dropdown. Populated later.</summary>
    [ObservableProperty]
    public partial ObservableCollection<FilterOption> FilterOptions { get; set; } = [];

    /// <summary>The currently selected filter options (bound to the combobox checkboxes).</summary>
    [ObservableProperty]
    public partial ObservableCollection<string> SelectedFilters { get; set; } = [];

    /// <summary>Schema options currently included in the search.</summary>
    [ObservableProperty]
    public partial ObservableCollection<ISchemaOption> SelectedSchemaFilters { get; set; }

    /// <summary>Summary shown in the collapsed filter combobox.</summary>
    public string FilterSummary => SelectedFilters.Count == FilterOptions.Count
        ? "Filters"
        : $"{SelectedFilters.Count} filter(s) selected";

    public string SchemaSummary =>
        SelectedSchemaFilters.Count == SchemaOptions.Count
            ? "Schemas"
            : $"{SelectedSchemaFilters.Count} schema(s) selected";

    /// <summary>
    /// Refreshes the filter summary when the selected-filter collection is replaced.
    /// </summary>
    partial void OnSelectedFiltersChanged(ObservableCollection<string> value)
    {
        OnPropertyChanged(nameof(FilterSummary));
    }

    partial void OnSelectedSchemaFiltersChanged(ObservableCollection<ISchemaOption> value)
    {
        OnPropertyChanged(nameof(SchemaSummary));
    }


    /// <summary>Toggles whether a schema option is part of the current selection.</summary>
    [RelayCommand]
    private void ToggleSchemaFilter(ISchemaOption schemaOption)
    {
        var isRemoved = SelectedSchemaFilters.Remove(schemaOption);

        if (schemaOption is AllSchemas)
        {
            SelectedSchemaFilters = isRemoved ? [] : [.. SchemaOptions];
        }
        else if (!isRemoved)
        {
            SelectedSchemaFilters.Add(schemaOption);
        }

        if (schemaOption is not AllSchemas)
        {
            var allSchemas = SchemaOptions.OfType<AllSchemas>().FirstOrDefault();
            if (allSchemas is not null)
                SelectedSchemaFilters.Remove(allSchemas);
            if (SelectedSchemaFilters.Count == SchemaOptions.Count - 1)
                SelectedSchemaFilters.Add(SchemaOptions.OfType<AllSchemas>().First());
        }

        OnPropertyChanged(nameof(SelectedSchemaFilters));
        OnPropertyChanged(nameof(SchemaSummary));

        if (SearchCommand.CanExecute(null))
            SearchCommand.Execute(null);
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
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private partial ObservableCollection<SearchResultRow> Results { get; set; } = [];

    /// <summary>
    /// Gets or sets the result rows that match the active search and type filters.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<SearchResultRow> FilteredResults { get; set; } = [];

    /// <summary>The currently selected result row.</summary>
    [NotifyCanExecuteChangedFor(nameof(GenerateCodeCommand))]
    [ObservableProperty]
    public partial SearchResultRow? SelectedResult { get; set; }

    /// <summary>Detail text shown in the read-only panel for the selected result.</summary>
    [ObservableProperty]
    public partial string DetailsText { get; set; } = string.Empty;

    /// <summary>Paths of dacpac files chosen via File ▸ Open dacpac.</summary>
    public ObservableCollection<string> OpenedDacpacFiles { get; } = [];

    /// <summary>
    /// Gets or sets whether a background operation is active.
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Gets or sets the message shown by the loading overlay.
    /// </summary>
    [ObservableProperty]
    public partial string LoadingMessage { get; set; } = "Loading…";

    /// <summary>
    /// Gets or sets the detail view model for the selected result.
    /// </summary>
    [ObservableProperty]
    public partial IDisplayViewModel Detail { get; set; }

    /// <summary>
    /// Gets the sample hierarchy displayed beneath the search results.
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<ITreeItem> TreeItems { get; set; }

    [ObservableProperty] public partial ObservableCollection<ISchemaOption> SchemaOptions { get; set; } = [];


    /// <summary>
    /// Updates the page's close availability when close prevention changes.
    /// </summary>
    partial void OnPreventCloseChanged(bool value)
    {
        CanClose = !value;
    }

    /// <summary>
    /// Determines whether the opened dacpac files can be installed.
    /// </summary>
    private bool CanExecuteInstall()
    {
        return OpenedDacpacFiles.Count > 0;
    }

    /// <summary>
    /// Opens the installation workflow for the loaded dacpac files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanExecuteInstall))]
    private async Task Install()
    {
        await this.Messenger.Send(new OpenInstallationMessage(OpenedDacpacFiles.Select(AbsolutePath.Create).ToArray()));
    }

    /// <summary>
    /// Creates the appropriate detail view model for the selected result.
    /// </summary>
    partial void OnSelectedResultChanged(SearchResultRow? value)
    {
        if (value is null) return;
        SetDetails(value.Source);
    }

    /// <summary>
    /// Creates the appropriate detail view model for the supplied SQL object.
    /// </summary>
    internal void SetDetails(TSqlObject sqlObject)
    {   
        if (sqlObject.ObjectType == Table.TypeClass)
        {
            Detail = new TableDisplayViewModel(sqlObject);
        }
        else if (sqlObject.ObjectType == TableType.TypeClass)
        {
            Detail = new TableTypeDisplayViewModel(sqlObject);
        }
        else if (sqlObject.ObjectType == Procedure.TypeClass)
        {
            Detail = new ProcedureDisplayViewModel(sqlObject);
        }
        else if (sqlObject.ObjectType == View.TypeClass)
        {
            Detail = new ViewDisplayViewModel(sqlObject);
        }
        else
        {
            Detail = new DefaultDisplayViewModel(sqlObject);
        }
    }

    /// <summary>
    /// Determines whether a result row matches the current free-text search.
    /// </summary>
    private bool SearchFilter(SearchResultRow row)
    {
        return row.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether loaded results are available to search.
    /// </summary>
    private bool CanSearch() => Results.Count > 0;

    /// <summary>
    /// Applies the selected object-type filters and free-text query to the loaded results.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        FilteredResults =
        [
            .. Results
                .Where(x => SelectedFilters.Contains(x.Type))
                .Where(SchemaFilter)
                .Where(SearchFilter)
        ];
    }

    private bool SchemaFilter(SearchResultRow row)
    {
        if (SelectedSchemaFilters.Count == 0)
            return false;

        if (SelectedSchemaFilters.OfType<AllSchemas>().Any())
        {
            return true;
        }

        var schemaName = row.Schema;
        if (schemaName == null)
        {
            return false;
        }

        if (schemaName.Parts.LastOrDefault() == "dbo")
            return SelectedSchemaFilters.OfType<DefaultSchema>().Any();

        return SelectedSchemaFilters.OfType<SchemaWrapped>()
            .Any(schemaWrapped => ObjectIdentifierComparer.Equals(schemaWrapped.Wrapped.SqlObject.Name, schemaName));
    }

    /// <summary>
    /// Determines whether code can be generated for the supplied selection.
    /// </summary>
    private bool CanGenerateCode(IList? items) => items is {Count: > 0};

    /// <summary>Copies the generated code for the selected results to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerateCode))]
    private async Task GenerateCode(IList? items)
    {
        var rows = items?.OfType<SearchResultRow>()
            .Where(x => x.GeneratorSupported)
            .ToArray() ?? [];
        if (rows.Length == 0)
        {
            SetStatusMessage("No selected objects support code generation.");
            return;
        }

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

    /// <summary>
    /// Prompts for dacpac files and loads the selected files.
    /// </summary>
    [RelayCommand]
    private async Task OpenDacpac()
    {
        var files = await _filePicker.PickDacpacFilesAsync();
        if (files.Count == 0)
            return;

        await OpenDacpacFilesAsync(files.Select(AbsolutePath.Create).ToList());
    }

    private readonly DacQueryScopes _dacQueryScopes = DacQueryScopes.UserDefined;

    /// <summary>
    /// Loads the supplied dacpac files and records them as a recent open operation.
    /// </summary>
    /// 
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

            var loadResult = await Task.Run(() =>
            {
                var source = _loader.LoadMultiple(uniqueFiles).ToList();

                _supportedObjectTypes = _builder.GetSupportedObjectTypes();

                var schemas = source
                    .SelectMany(x => x.Model.GetObjects(_dacQueryScopes, Schema.TypeClass)
                        .DistinctBy(y => y.Name.ToString()))
                    .Select(x => x.ToSchema())
                    .ToList();

                var schemaOptions = new List<ISchemaOption>
                {
                    new AllSchemas(),
                    new DefaultSchema()
                };
                schemaOptions.AddRange(schemas.Select(x => new SchemaWrapped(x)));

                var rows = source
                    .SelectMany(x =>
                        x.Model.GetObjects(_dacQueryScopes).Select(y => new {ObjectName = y, x.Path}))
                    .Where(x => x.ObjectName.Name.HasName)
                    .Select(x => new SearchResultRow(
                        x.ObjectName,
                        x.Path.GetFilenameWithoutExtension(),
                        _supportedObjectTypes.Contains(x.ObjectName.ObjectType),
                        x.ObjectName.GetSchema()))
                    .ToList();

                TreeItems = [.. _treeDisplayService.GetRoots(source.Select(x => x.Model))];

                return (Rows: rows, SchemaOptions: schemaOptions);
            });

            OpenedDacpacFiles.AddRange(uniqueFiles.Select(x => x.Value));
            _settingsService.SaveOrUpdatePaths(uniqueFiles);

            searchResultRows.AddRange(loadResult.Rows);

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
            FilteredResults = [.. Results];
            FilterOptions = [new FilterOption("All", false), .. filterOptions];
            SelectedFilters = ["All", .. filterOptions.Select(option => option.Type)];
            SchemaOptions = new ObservableCollection<ISchemaOption>(loadResult.SchemaOptions);
            SelectedSchemaFilters = [.. SchemaOptions];
            SetStatusMessage($"Opened {files.Count} dacpac file(s).");
            InstallCommand.NotifyCanExecuteChanged();
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Verifies that every supplied dacpac path exists and reports missing files.
    /// </summary>
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

    /// <summary>
    /// Records activation of the landing page.
    /// </summary>
    public override Task OnActivatedAsync()
    {
        _logger.LogInformation("On Activated");
        IsActive = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records closure of the landing page.
    /// </summary>
    public override Task CloseAsync()
    {
        _logger.LogInformation("On Close");
        return Task.CompletedTask;
    }

    public void Receive(ThemeChangedMessage message)
    {
        foreach (var result in FilteredResults)
        {
            result.TriggerGeneratorSupportedChanged();
        }
    }
}
