using System.Diagnostics;
using System.Runtime.InteropServices;
using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Opens local folders with the platform's native file manager.
/// </summary>
public sealed class FolderLauncher : IFolderLauncher
{
    /// <summary>
    /// Opens a local folder in Windows Explorer or the Linux desktop's default file manager.
    /// </summary>
    public void Open(AbsolutePath folder)
    {
        var fileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "explorer.exe" : "xdg-open";
        Process.Start(new ProcessStartInfo(fileName, folder.Value) {UseShellExecute = true});
    }
}