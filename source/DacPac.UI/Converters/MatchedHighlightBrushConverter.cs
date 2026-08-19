using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DacPac.UI.Converters;

/// <summary>
/// Converts search-match state to its highlight brush.
/// </summary>
public class MatchedHighlightBrushConverter : IValueConverter
{
    public static readonly MatchedHighlightBrushConverter Instance = new();

    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.Parse("#3300BFFF")); // semi-transparent cyan
    private static readonly IBrush TransparentBrush = Brushes.Transparent;

    /// <summary>
    /// Converts a match state to a highlight brush.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? HighlightBrush : TransparentBrush;

    /// <summary>
    /// Converts a brush value back to a match state when supported.
    /// </summary>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
