using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DacPac.UI.Converters;

/// <summary>
/// Provides a light green row background for results that support code generation.
/// </summary>
public sealed class GeneratorSupportedBrushConverter : IValueConverter
{
    public static GeneratorSupportedBrushConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Brushes.LightGreen : AvaloniaProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
