using System.IO.Abstractions;
using System.Text.Json.Serialization.Metadata;
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