using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DacPac.UI.Converters;

/// <summary>
/// Reports whether a tree item has a resolvable icon resource.
/// </summary>
public sealed class TreeItemHasIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return TreeItemIconConverter.TryGetIcon(value, out _);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
