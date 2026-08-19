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
    /// <summary>
    /// Gets the shared converter instance.
    /// </summary>
    public static GeneratorSupportedBrushConverter Instance { get; } = new();

    /// <summary>
    /// Resolves the supported-generator brush when the value is <see langword="true"/>.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not true)
            return AvaloniaProperty.UnsetValue;

        var application = Application.Current;
        return application?.TryFindResource("GeneratorSupportedRowBrush", application.ActualThemeVariant, out var brush) == true
            ? brush
            : AvaloniaProperty.UnsetValue;
    }

    /// <summary>
    /// Returns a no-op result because reverse conversion is unsupported.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
