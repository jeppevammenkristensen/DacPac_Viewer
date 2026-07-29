namespace DacPac.UI.ApplicationLayer.Infrastructure;

/// <summary>
/// Provides stable machine identity material for non-interactive encryption.
/// </summary>
public interface IMachineIdentityProvider
{
    /// <summary>
    /// Gets the current machine's stable identity.
    /// </summary>
    string GetMachineIdentity();
}