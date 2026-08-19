using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Persists settings data as JSON at a configured file path.
/// </summary>
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
    
    /// <summary>
    /// Loads persisted settings, or creates an empty value when they cannot be read.
    /// </summary>
    public TData Load()
    {
        try
        {
            if (!_filePath.FileExists(_fileSystem)) return _createEmpty();
            var json = _filePath.ReadAllText(_fileSystem);
            return JsonSerializer.Deserialize(json, _typeInfo) ?? _createEmpty();
        }
        catch (Exception)
        {
            return _createEmpty();
        }
    }
    /// <summary>
    /// Saves the supplied settings value as JSON.
    /// </summary>
    public void Save(TData data)
    {
        try
        {
            var directory = _filePath / "..";
            directory.CreateDirectory(_fileSystem);
            _fileSystem.File.WriteAllText(_filePath, JsonSerializer.Serialize(data, _typeInfo));
        }
        catch (Exception)
        {
        }
    }
}
