using System.IO.Abstractions;
using System.Text.Json.Serialization.Metadata;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Provides helpers for creating JSON-backed settings wrappers.
/// </summary>
public static class JsonSettingsExtensions
{
    /// <summary>
    /// Creates a settings wrapper from source-generated JSON metadata.
    /// </summary>
    public static JsonSettingsWrapper<TData> WrapperFromTypeInfo<TData>(this JsonTypeInfo<TData> typeInfo,
        AbsolutePath filePath, IFileSystem fileSystem, Func<TData> createEmpty)
    {
        return new JsonSettingsWrapper<TData>(filePath, typeInfo, fileSystem, createEmpty);
    }
}
