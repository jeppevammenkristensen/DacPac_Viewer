using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Default <see cref="IFilePickerService"/> backed by the desktop main window's
/// <see cref="IStorageProvider"/>.
/// </summary>
public class StorageProviderFilePickerService : IFilePickerService
{
    public async Task<IReadOnlyList<string>> PickDacpacFilesAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime
            {
                MainWindow.StorageProvider: { } storageProvider
            })
        {
            return [];
        }

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open dacpac",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("DacPac") { Patterns = ["*.dacpac"] }
            ]
        });

        var paths = new List<string>();
        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
                paths.Add(path);
        }

        return paths;
    }
}
