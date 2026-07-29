using TruePath;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Opens local folders in the operating system's default file manager.
/// </summary>
public interface IFolderLauncher
{
    /// <summary>
    /// Opens a local folder in the default file manager.
    /// </summary>
    void Open(AbsolutePath folder);
}