using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DacPac.UI.Converters;


public class MatchedHighlightBrushConverter : IValueConverter
{
    public static readonly MatchedHighlightBrushConverter Instance = new();

    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#3300BFFF")); // semi-transparent cyan
    private static readonly IBrush TransparentBrush = Brushes.Transparent;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? HighlightBrush : TransparentBrush;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Resolves the active theme's supported-generator row brush.
/// </summary>
public sealed class GeneratorSupportedBrushConverter : IValueConverter
{
    public static GeneratorSupportedBrushConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
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
