using System.IO.Abstractions;

namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Reads the Linux machine identity used to derive a local encryption key.
/// </summary>
public sealed class LinuxMachineIdentityProvider(IFileSystem fileSystem) : IMachineIdentityProvider
{
    /// <summary>
    /// Gets the Linux machine ID from <c>/etc/machine-id</c>.
    /// </summary>
    public string GetMachineIdentity()
    {
        const string machineIdPath = "/etc/machine-id";

        if (!fileSystem.File.Exists(machineIdPath))
        {
            throw new InvalidOperationException("Unable to read the Linux machine identity.");
        }

        return fileSystem.File.ReadAllText(machineIdPath).Trim();
    }
}