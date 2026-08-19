using FileBasedApp.Toolkit;
using TruePath;
using DacPac.Core;

namespace DacPac.UI.ApplicationLayer;

/// <summary>
/// Provides the application's persistent and temporary storage locations.
/// </summary>
public class FileLocations : IFileLocations
{
    /// <summary>
    /// Gets the directory used for persistent application data.
    /// </summary>
    public AbsolutePath RootSaveLocation => Environment.SpecialFolder.LocalApplicationData.GetSpecialFolder() / "DacPacViewer";

    /// <summary>
    /// Gets the directory used for temporary staged DacPac files.
    /// </summary>
    public AbsolutePath TempSaveLocation => RootSaveLocation / "TempDacPacs";
}
