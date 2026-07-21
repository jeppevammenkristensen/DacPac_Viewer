using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using DacPac.Core;
using Microsoft.Extensions.Logging;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

public static class JsonSettingsExtensions
{
    public static JsonSettingsWrapper<TData> WrapperFromTypeInfo<TData>(this JsonTypeInfo<TData> typeInfo,
        AbsolutePath filePath, IFileSystem fileSystem, Func<TData> createEmpty)
    {
        return new JsonSettingsWrapper<TData>(filePath, typeInfo, fileSystem, createEmpty);
    }
}

public class JsonSettingsWrapper<TData>
{
    private readonly AbsolutePath _filePath;
    private readonly JsonTypeInfo<TData> _typeInfo;
    private readonly IFileSystem _fileSystem;
    private readonly Func<TData> _createEmpty;

    public JsonSettingsWrapper(AbsolutePath filePath, JsonTypeInfo<TData> typeInfo, IFileSystem fileSystem, Func<TData> createEmpty)
    {
        _filePath = filePath;
        _typeInfo = typeInfo;
        _fileSystem = fileSystem;
        _createEmpty = createEmpty;
    }
    
    public TData Load()
    {
        try
        {
            if (!_filePath.FileExists(_fileSystem)) return _createEmpty();
            var json = _filePath.ReadAllText(_fileSystem);
            return JsonSerializer.Deserialize(json, _typeInfo) ?? _createEmpty();
        }
        catch (Exception ex)
        {
            return _createEmpty();
        }
    }
    public void Save(TData data)
    {
        try
        {
            var directory = _filePath / "..";
            directory.CreateDirectory(_fileSystem);
            _fileSystem.File.WriteAllText(_filePath, JsonSerializer.Serialize(data, _typeInfo));
        }
        catch (Exception ex)
        {
        }
    }
}


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
        _storedPathsWrapper = StoredPathsJsonContext.Default.StoredPaths.WrapperFromTypeInfo(_fileLocations.RootSaveLocation / "storedpaths.json", _fileSystem, () => new StoredPaths(ImmutableArray<StoredPath>.Empty));
    }

    private AbsolutePath SettingsFilePath => _fileLocations.RootSaveLocation / "settings.json";

    private readonly ILogger<JsonFileSettingsService> _logger;
    private readonly SettingsData _data;
    private JsonSettingsWrapper<StoredPaths> _storedPathsWrapper;

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

    public void SaveOrUpdatePaths(IReadOnlyList<AbsolutePath> paths)
    {
        var storedPaths = _storedPathsWrapper.Load();
        var storedPath = new StoredPath(paths.Select(x => x.Value).ToArray());
        ImmutableArray<StoredPath> newPath = [storedPath,..storedPaths.Paths.Where(x => !x.Equals(storedPath))];
        
        storedPaths = storedPaths with { Paths = newPath };
        _storedPathsWrapper.Save(storedPaths);
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

    private record StoredPaths(ImmutableArray<StoredPath> Paths) {
        
    }

    private record StoredPath(string[] Path)
    {
        public virtual bool Equals(StoredPath? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Path.SequenceEqual(other.Path);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(StoredPaths))]
    private partial class StoredPathsJsonContext : JsonSerializerContext
    {
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(SettingsData))]
    private partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}
