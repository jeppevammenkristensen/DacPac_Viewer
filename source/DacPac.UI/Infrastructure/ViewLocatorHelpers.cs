using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace DacPac.UI.Infrastructure;

/// <summary>
/// Provides dependency-injection registration helpers for view models and their views.
/// </summary>
public static class ViewLocatorHelpers
{
    internal static IServiceCollection AddViewModelAndRegisterView<TViewModel, TView>(
        this IServiceCollection collection, ViewModelScope scope)
        where TViewModel : ObservableObject where TView : Control, new()
    {
        switch (scope)
        {
            case ViewModelScope.Transient:
                collection.AddTransient<TViewModel>();
                break;
            case ViewModelScope.Singleton:
                collection.AddSingleton<TViewModel>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
        }

        return collection.AddView<TViewModel, TView>();
    }

    /// <summary>
    /// Registers a view factory for a view-model type.
    /// </summary>
    public static IServiceCollection AddView<TViewModel, TView>(this IServiceCollection collection)
        where TViewModel : ObservableObject where TView : Control, new()
    {
        collection.AddSingleton(new ViewLocatorDescriptor(typeof(TViewModel), () => new TView()));
        return collection;
    }
}
