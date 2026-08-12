using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DacPac.UI.Converters;

/// <summary>
/// Resolves a tree item's icon identifier to its application vector resource.
/// </summary>
public sealed class TreeItemIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return TryGetIcon(value, out var icon) ? icon : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }

    internal static bool TryGetIcon(object? value, out object? icon)
    {
        icon = null;
        if (value is not string iconId || string.IsNullOrWhiteSpace(iconId))
            return false;

        var application = Application.Current;
        return application?.TryFindResource($"{iconId}Icon", application.ActualThemeVariant, out icon) == true;
    }
}
