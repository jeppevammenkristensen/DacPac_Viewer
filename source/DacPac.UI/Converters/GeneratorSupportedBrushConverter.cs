using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace DacPac.UI.Converters;

/// <summary>
/// Resolves the active theme's supported-generator row brush.
/// </summary>
public sealed class GeneratorSupportedBrushConverter : IValueConverter
{
    public static GeneratorSupportedBrushConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
            return AvaloniaProperty.UnsetValue;

        var application = Application.Current;
        return application?.TryFindResource("GeneratorSupportedRowBrush", application.ActualThemeVariant, out var brush) == true
            ? brush
            : AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
