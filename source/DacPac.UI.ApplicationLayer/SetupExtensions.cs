using DacPac.Core;
using DacPac.UI.ApplicationLayer.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DacPac.UI.ApplicationLayer;

public static class SetupExtensions
{
    public static void SetupApplicationLayerService(this IServiceCollection services)
    {
        services.AddSingleton<IFileLocations, FileLocations>();
        services.AddSingleton<ISettingsService, JsonFileSettingsService>();
    }
}
