using FileBasedApp.Toolkit;
using TruePath;
using DacPac.Core;

namespace DacPac.UI.ApplicationLayer;

public class FileLocations : IFileLocations
{
    public AbsolutePath RootSaveLocation => Environment.SpecialFolder.LocalApplicationData.GetSpecialFolder() / "DacPacViewer";
    public AbsolutePath TempSaveLocation => RootSaveLocation / "TempDacPacs";
}
