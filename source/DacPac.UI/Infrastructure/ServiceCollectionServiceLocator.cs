using System;
using Microsoft.Extensions.DependencyInjection;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Resolves services from a dependency-injection service provider.
/// </summary>
public class ServiceCollectionServiceLocator(IServiceProvider services) : IServiceLocator
{
    /// <summary>
    /// Gets a service when it is registered.
    /// </summary>
    public T? GetService<T>()
    {
        return services.GetService<T>();
    }

    /// <summary>
    /// Gets a required registered service.
    /// </summary>
    public T GetRequiredService<T>() where T : notnull
    {
        return services.GetRequiredService<T>();
    }
}
