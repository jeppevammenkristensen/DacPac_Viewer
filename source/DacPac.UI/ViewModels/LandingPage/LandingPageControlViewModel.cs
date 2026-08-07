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
    [NotifyCanExecuteChangedFor(nameof(ResetSearchCommand))]
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

    /// <summary>
    /// Gets or sets the currently selected tree item.
    /// </summary>
    [NotifyCanExecuteChangedFor(nameof(CopyTreeItemNameCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerateTreeItemCodeCommand))]
    [ObservableProperty]
    public partial ObservableCollection<ITreeItem> SelectedTreeItem { get; set; } = [];

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
    private async Task Search()
    {
        FilteredResults =
        [
            .. Results
                .Where(x => SelectedFilters.Contains(x.Type))
                .Where(SchemaFilter)
                .Where(SearchFilter)
        ];

        IsLoading = true;
        try
        {
            LoadingMessage = "Filtering";
            await Task.Run(FilterTree);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Clears the free-text query and reapplies the active filters.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetSearch))]
    private async Task ResetSearch()
    {
        SearchText = string.Empty;
        await Search();
    }

    /// <summary>
    /// Determines whether the free-text query can be reset.
    /// </summary>
    private bool CanResetSearch() => !string.IsNullOrEmpty(SearchText) && CanSearch();

    private void FilterTree()
    {
        foreach (var treeItem in TreeItems)
        {
            treeItem.Traverse(x =>
            {
                x.IsHidden = false;
                x.IsMatch = false;
                x.IsExpanded = false;
            });
        }
        

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            if (SelectedSchemaFilters.Count == SchemaOptions.Count && SelectedFilters.Count == FilterOptions.Count)
            {
                ExpandAllTreeItemsCommand.Execute(null);
                return;
            }
        }
        
        foreach (var treeItem in TreeItems)
        {
            SetExpanded(treeItem, isExpanded: false, includeSqlObjects: true);
            
            if (treeItem is SchemaTreeItem sqlObjectTreeItem)
            {
                if (!SchemaFilter(sqlObjectTreeItem.Identifier))
                {
                    treeItem.IsHidden = true;
                    continue;
                }
            }
            
            FilterTreeItem(treeItem);
        }
    }

    private (bool isMatch, bool isDirectMatch) FilterTreeItem(ITreeItem treeItem, bool hasSqlObjectAncestor = false)
    {
        bool isMatch = false;
        bool childIsDirectMatch = false;
        bool isRootMatch = false;
        
        if (treeItem is ISqlObjectTreeItem sqlObjectTreeItem)
        {
            var (match, directMatch) = IsMatch(sqlObjectTreeItem.Source);
            isMatch = match;

            sqlObjectTreeItem.IsHidden = !isMatch;
            if (hasSqlObjectAncestor)
            {
                treeItem.IsHidden = false;
            }
            
            sqlObjectTreeItem.IsMatch = directMatch;
            childIsDirectMatch = directMatch;
            isRootMatch = true;
        }

        foreach (var child in treeItem.Children)
        {
            var (currentIsMatch, currentIsDirectMatch) = FilterTreeItem(child, isRootMatch || hasSqlObjectAncestor);

            if (currentIsMatch)
            {
                isMatch = true;
            }

            if (currentIsDirectMatch)
            {
                childIsDirectMatch = true;
            }
        }

        treeItem.IsHidden = !isMatch;
        if (hasSqlObjectAncestor)
        {
            treeItem.IsHidden = false;
        }
        
        treeItem.IsExpanded = (isMatch && !hasSqlObjectAncestor) || childIsDirectMatch;

        return (isMatch, childIsDirectMatch);
    }

    private (bool match, bool directMatch) IsMatch(TSqlObject source)
    {
        
        if (!SelectedFilters.Contains(source.ObjectType.Name))
        {
            return (false,false);
        }
        
        var isTextMatch = (source.Name.Parts?.Last() ?? string.Empty).Contains(this.SearchText, StringComparison.OrdinalIgnoreCase);
        return (isTextMatch, isTextMatch && !string.IsNullOrWhiteSpace(this.SearchText));   
        
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
    
    private bool SchemaFilter(ObjectIdentifier? row)
    {
        if (SelectedSchemaFilters.Count == 0)
            return false;

        if (SelectedSchemaFilters.OfType<AllSchemas>().Any())
        {
            return true;
        }       
        
        if (row == null)
        {
            return false;
        }

        if (row.Parts.LastOrDefault() == "dbo")
            return SelectedSchemaFilters.OfType<DefaultSchema>().Any();

        return SelectedSchemaFilters.OfType<SchemaWrapped>()
            .Any(schemaWrapped => ObjectIdentifierComparer.Equals(schemaWrapped.Wrapped.SqlObject.Name, row));
    }

    /// <summary>
    /// Determines whether code can be generated for the supplied selection.
    /// </summary>
    private bool CanGenerateCode(IList? items) => items is {Count: > 0};

    private bool CanCopyTreeItemName() => SelectedTreeItem is { Count: 1};

    /// <summary>Copies the selected tree item's name to the clipboard.</summary>
    [RelayCommand(CanExecute = nameof(CanCopyTreeItemName))]
    private async Task CopyTreeItemName()
    {
        if (SelectedTreeItem is { Count: 0})
            return;

        await _clipboard.SetTextAsync(SelectedTreeItem[0].Name);
        SetStatusMessage($"Copied {SelectedTreeItem[0].Name} to the clipboard.");
    }

    private bool CanGenerateTreeItemCode() => SelectedTreeItem?.OfType<ISqlObjectTreeItem>().ToList() is
        {Count: > 0};

    /// <summary>
    /// Expands non-SQL grouping nodes throughout the object tree.
    /// </summary>
    [RelayCommand]
    private void ExpandAllTreeItems()
    {
        foreach (var item in TreeItems)
        {
            SetExpanded(item, isExpanded: true, includeSqlObjects: false);
        }
    }

    /// <summary>
    /// Expands all descendants of the selected tree items.
    /// </summary>
    [RelayCommand]
    private void ExpandSelected()
    {
        foreach (var item in SelectedTreeItem)
        {
            SetExpanded(item, isExpanded: true, includeSqlObjects: true);
        }
    }

    /// <summary>
    /// Collapses the selected tree items and all their descendants.
    /// </summary>
    [RelayCommand]
    private void CollapseSelected()
    {
        foreach (var item in SelectedTreeItem)
        {
            SetExpanded(item, isExpanded: false, includeSqlObjects: true);
        }
    }

    /// <summary>
    /// Updates expansion state recursively without depending on generated tree controls.
    /// </summary>
    private static void SetExpanded(ITreeItem item, bool isExpanded, bool includeSqlObjects)
    {
        if (item is not ISqlObjectTreeItem || includeSqlObjects)
        {
            item.IsExpanded = isExpanded;
        }

        foreach (var child in item.Children)
        {
            SetExpanded(child, isExpanded, includeSqlObjects);
        }
    }
                                              

    /// <summary>Generates code for the selected SQL object tree item.</summary>
    [RelayCommand(CanExecute = nameof(CanGenerateTreeItemCode))]
    private async Task GenerateTreeItemCode()
    {
        if (SelectedTreeItem?.OfType<ISqlObjectTreeItem>().ToList() is not {Count: >0} sqlObjectTreeItems)
            return;

        var treeItems = sqlObjectTreeItems.Select(treeItem => new SearchResultRow(treeItem.Source,
            string.Empty,
            _supportedObjectTypes.Contains(treeItem.Source.ObjectType),
            treeItem.Source.GetSchema())).ToList();

        await GenerateCode(treeItems);
    }

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
            if (!TryPrepareFiles(files, out var uniqueFiles))
            {
                return;
            }

            var loadResult = await Task.Run(() => LoadDacpacs(uniqueFiles));
            var filterOptions = await Task.Run(() => CreateFilterOptions(loadResult.Rows));
            ApplyLoadedDacpacs(files.Count, uniqueFiles, loadResult, filterOptions);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Validates the selected paths and clears state that belongs to a previous open operation.
    /// </summary>
    private bool TryPrepareFiles(IReadOnlyList<AbsolutePath> files, out List<AbsolutePath> uniqueFiles)
    {
        uniqueFiles = [];
        if (!CheckFiles(files))
        {
            _settingsService.RemovePaths(files);
            return false;
        }

        OpenedDacpacFiles.Clear();
        Results.Clear();
        uniqueFiles = files.Distinct().ToList();
        return true;
    }

    /// <summary>
    /// Loads DAC models and derives data required to populate the page.
    /// </summary>
    private LoadedDacpacs LoadDacpacs(IReadOnlyList<AbsolutePath> files)
    {
        var source = _loader.LoadMultiple(files).ToList();
        _supportedObjectTypes = _builder.GetSupportedObjectTypes();

        var schemaOptions = new List<ISchemaOption>
        {
            new AllSchemas(),
            new DefaultSchema()
        };
        schemaOptions.AddRange(source
            .SelectMany(x => x.Model.GetObjects(_dacQueryScopes, Schema.TypeClass)
                .DistinctBy(y => y.Name.ToString()))
            .Select(x => new SchemaWrapped(x.ToSchema())));

        var rows = source
            .SelectMany(x => x.Model.GetObjects(_dacQueryScopes).Select(y => new { ObjectName = y, x.Path }))
            .Where(x => x.ObjectName.Name.HasName)
            .Select(x => new SearchResultRow(
                x.ObjectName,
                x.Path.GetFilenameWithoutExtension(),
                _supportedObjectTypes.Contains(x.ObjectName.ObjectType),
                x.ObjectName.GetSchema()))
            .ToList();

        var treeItems = _treeDisplayService.GetRoots(source.Select(x => x.Model)).ToList();
        return new LoadedDacpacs(rows, schemaOptions, treeItems);
    }

    /// <summary>
    /// Creates selectable object-type filters from the loaded rows.
    /// </summary>
    private static List<FilterOption> CreateFilterOptions(IEnumerable<SearchResultRow> rows)
    {
        return rows
            .GroupBy(row => row.Type)
            .Select(group => new FilterOption(group.Key, group.Any(row => row.GeneratorSupported)))
            .OrderBy(option => option.Type)
            .ToList();
    }

    /// <summary>
    /// Applies a completed load operation to the page state.
    /// </summary>
    private void ApplyLoadedDacpacs(int fileCount,
        IReadOnlyList<AbsolutePath> files,
        LoadedDacpacs loadResult,
        IReadOnlyList<FilterOption> filterOptions)
    {
        OpenedDacpacFiles.AddRange(files.Select(x => x.Value));
        _settingsService.SaveOrUpdatePaths(files);
        CurrentTitle = string.Join(",", OpenedDacpacFiles.Select(AbsolutePath.Create).Select(x => x.FileName));
        TreeItems = [.. loadResult.TreeItems];
        Results = new ObservableCollection<SearchResultRow>(loadResult.Rows);
        FilteredResults = [.. Results];
        FilterOptions = [new FilterOption("All", false), .. filterOptions];
        SelectedFilters = ["All", .. filterOptions.Select(option => option.Type)];
        SchemaOptions = new ObservableCollection<ISchemaOption>(loadResult.SchemaOptions);
        SelectedSchemaFilters = [.. SchemaOptions];
        SetStatusMessage($"Opened {fileCount} dacpac file(s).");
        InstallCommand.NotifyCanExecuteChanged();
        ExpandAllTreeItemsCommand.Execute(null);
    }

    private sealed record LoadedDacpacs(
        IReadOnlyList<SearchResultRow> Rows,
        IReadOnlyList<ISchemaOption> SchemaOptions,
        IReadOnlyList<ITreeItem> TreeItems);


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
