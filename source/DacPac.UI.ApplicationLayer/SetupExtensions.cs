using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DacPac.UI.ApplicationLayer;

/// <summary>
/// Registers application-layer services with dependency injection.
/// </summary>
public static class SetupExtensions
{
    /// <summary>
    /// Adds the application-layer service implementations to the collection.
    /// </summary>
    public static void SetupApplicationLayerService(this IServiceCollection services)
    {
        services.AddSingleton<IFileLocations, FileLocations>();
        services.AddSingleton<IMachineIdentityProvider, LinuxMachineIdentityProvider>();
        services.AddSingleton<IStringEncrypter, StringEncrypter>();
        services.AddSingleton<ISettingsService, JsonFileSettingsService>();
        services.AddSingleton<IErrorCollector, ErrorCollector>();
    }
}
