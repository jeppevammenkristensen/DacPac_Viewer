using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

public partial class JsonFileSettingsService : ISettingsService
{
    private readonly IFileSystem _fileSystem;
    private readonly IFileLocations _fileLocations;


    public JsonFileSettingsService(IFileSystem fileSystem, IFileLocations fileLocations, ILogger<JsonFileSettingsService> logger)
    {
        _fileSystem = fileSystem;
        _fileLocations = fileLocations;
        _logger = logger;
        _data = Load();
    }

    private AbsolutePath SettingsFilePath => _fileLocations.RootSaveLocation / "settings.json";

    private readonly ILogger<JsonFileSettingsService> _logger;
    private readonly SettingsData _data;

    public bool EnableBetaUpdates
    {
        get => _data.EnableBetaUpdates;
        set
        {
            if (_data.EnableBetaUpdates == value) return;
            _data.EnableBetaUpdates = value;
            Save();
        }
    }

    private SettingsData Load()
    {
        try
        {
            if (!SettingsFilePath.FileExists(_fileSystem)) return new SettingsData();
            var json = _fileSystem.File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsData) ?? new SettingsData();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings from {Path}; using defaults", SettingsFilePath);
            return new SettingsData();
        }
    }

    private void Save()
    {
        try
        {
            var directory = SettingsFilePath / "..";
            directory.CreateDirectory(_fileSystem);
            _fileSystem.File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(_data, SettingsJsonContext.Default.SettingsData));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings to {Path}", SettingsFilePath);
        }
    }

    private  class SettingsData
    {
        public bool EnableBetaUpdates { get; set; }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(SettingsData))]
    private partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}
