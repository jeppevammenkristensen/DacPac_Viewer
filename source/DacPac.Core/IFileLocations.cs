using TruePath;

namespace DacPac.Core;

/// <summary>
/// Defines the application-owned locations used for persisted settings and temporary DACPAC staging.
/// </summary>
public interface IFileLocations
{
    AbsolutePath RootSaveLocation { get; }
    
    AbsolutePath TempSaveLocation { get; }
}
