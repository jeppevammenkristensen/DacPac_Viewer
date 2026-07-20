using TruePath;

namespace DacPac.UI.ApplicationLayer;

public interface IFileLocations
{
    AbsolutePath RootSaveLocation { get; }
    
    AbsolutePath TempSaveLocation { get; }
}