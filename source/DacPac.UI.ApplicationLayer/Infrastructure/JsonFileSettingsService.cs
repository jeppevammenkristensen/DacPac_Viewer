using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using DacPac.Core;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Persists application settings in JSON files.
/// </summary>
public partial class JsonFileSettingsService : ISettingsService
{
    private readonly IFileSystem _fileSystem;
    private readonly IFileLocations _fileLocations;


    /// <summary>
    /// Initializes a JSON-backed settings service.
    /// </summary>
    public JsonFileSettingsService(IFileSystem fileSystem,
        IFileLocations fileLocations,
        ILogger<JsonFileSettingsService> logger,
        IStringEncrypter encrypter,
        IMessenger messenger)
    {
        _fileSystem = fileSystem;
        _fileLocations = fileLocations;
        _logger = logger;
        _encrypter = encrypter;
        _messenger = messenger;
        _data = Load();
        _storedPathsWrapper = StoredPathsJsonContext.Default.StoredPaths.WrapperFromTypeInfo(
            _fileLocations.RootSaveLocation / "storedpaths.json", _fileSystem,
            () => new StoredPaths(ImmutableArray<StoredPath>.Empty));
    }

    private AbsolutePath SettingsFilePath => _fileLocations.RootSaveLocation / "settings.json";

    private readonly ILogger<JsonFileSettingsService> _logger;
    private readonly IStringEncrypter _encrypter;
    private readonly IMessenger _messenger;
    private readonly SettingsData _data;
    private JsonSettingsWrapper<StoredPaths> _storedPathsWrapper;

    /// <summary>
    /// Gets or sets whether update checks include beta releases.
    /// </summary>
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

    public bool StoreConnectionStrings
    {
        get => _data.PersistLatestConnectionString ?? false;
        set
        {
            if (_data.PersistLatestConnectionString == value) return;
            _data.PersistLatestConnectionString = value;
            if (value == false)
            {
                _data.EncryptedLatestConnectionString = null;
            }

            Save();
        }
    }

    /// <summary>
    /// Gets or sets the latest connection string protected for the current Windows user.
    /// </summary>
    public string? LatestConnectionString
    {
        get
        {
            if (!StoreConnectionStrings)
                return null;

            if (string.IsNullOrWhiteSpace(_data.EncryptedLatestConnectionString)) return null;

            var connectionString = UnprotectConnectionString(_data.EncryptedLatestConnectionString);
            if (connectionString is not null) return connectionString;

            _data.EncryptedLatestConnectionString = null;
            Save();
            return null;
        }
        set
        {
            if (!StoreConnectionStrings)
                return;

            var protectedValue = string.IsNullOrEmpty(value) ? null : ProtectConnectionString(value);
            if (_data.EncryptedLatestConnectionString == protectedValue) return;

            _data.EncryptedLatestConnectionString = protectedValue;
            Save();
        }
    }

    /// <summary>
    /// Gets the saved DacPac path groups.
    /// </summary>
    public IReadOnlyList<AbsolutePath[]> GetStoredPaths()
    {
        var storedPaths = _storedPathsWrapper.Load();
        return storedPaths.Paths.Select(x => x.Path.Select(AbsolutePath.Create).ToArray()).ToList();
    }

    /// <summary>
    /// Removes a saved DacPac path group.
    /// </summary>
    public void RemovePaths(IReadOnlyList<AbsolutePath> files)
    {
        var storedPaths = _storedPathsWrapper.Load();
        var pathToRemove = new StoredPath([.. files.Select(x => x.Value)]);
        var newPath = storedPaths.Paths.Remove(pathToRemove);

        storedPaths = new StoredPaths(newPath);
        SaveNotitfyUpdatedStoredPaths(storedPaths);
    }

    /// <summary>
    /// Saves a DacPac path group and moves it to the front of the recent list.
    /// </summary>
    public void SaveOrUpdatePaths(IReadOnlyList<AbsolutePath> paths)
    {
        var storedPaths = _storedPathsWrapper.Load();
        var storedPath = new StoredPath(paths.Select(x => x.Value).ToArray());
        ImmutableArray<StoredPath> newPath = [storedPath, .. storedPaths.Paths.Where(x => !x.Equals(storedPath))];

        storedPaths = new StoredPaths(Paths: newPath);

        SaveNotitfyUpdatedStoredPaths(storedPaths);
    }

    /// <summary>
    /// Persists path groups and notifies subscribers of the updated list.
    /// </summary>
    private void SaveNotitfyUpdatedStoredPaths(StoredPaths storedPaths)
    {
        _storedPathsWrapper.Save(storedPaths);
        _messenger.Send(new StoredPathsChangedMessage(storedPaths.Paths
            .Select(x => x.Path.Select(AbsolutePath.Create).ToArray()).ToList()));
    }

    /// <summary>
    /// Loads settings from disk, returning defaults when they cannot be read.
    /// </summary>
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

    /// <summary>
    /// Persists the current settings to disk.
    /// </summary>
    private void Save()
    {
        try
        {
            var directory = SettingsFilePath / "..";
            directory.CreateDirectory(_fileSystem);
            _fileSystem.File.WriteAllText(SettingsFilePath,
                JsonSerializer.Serialize(_data, SettingsJsonContext.Default.SettingsData));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save settings to {Path}", SettingsFilePath);
        }
    }

    /// <summary>
    /// Encrypts a connection string using the current Windows user's DPAPI key.
    /// </summary>
    private string ProtectConnectionString(string connectionString)
    {
        return _encrypter.Encrypt(connectionString);
    }

    /// <summary>
    /// Decrypts a connection string stored by <see cref="ProtectConnectionString"/>.
    /// </summary>
    private string? UnprotectConnectionString(string encryptedConnectionString)
    {
        return _encrypter.Decrypt(encryptedConnectionString);
    }

    /// <summary>
    /// Represents the settings serialized to the main settings file.
    /// </summary>
    private class SettingsData
    {
        /// <summary>
        /// Gets or sets whether update checks include beta releases.
        /// </summary>
        public bool EnableBetaUpdates { get; set; }

        /// <summary>
        /// Gets or sets the DPAPI-protected latest connection string.
        /// </summary>
        public string? EncryptedLatestConnectionString { get; set; }

        /// <summary>
        /// Gets or sets whether the latest connection string should be persisted.
        /// </summary>
        public bool? PersistLatestConnectionString { get; set; }
    }

    private record StoredPaths(ImmutableArray<StoredPath> Paths)
    {
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

    /// <summary>
    /// Provides source-generated JSON metadata for stored paths.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(StoredPaths))]
    private partial class StoredPathsJsonContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Provides source-generated JSON metadata for application settings.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(SettingsData))]
    private partial class SettingsJsonContext : JsonSerializerContext
    {
    }
}