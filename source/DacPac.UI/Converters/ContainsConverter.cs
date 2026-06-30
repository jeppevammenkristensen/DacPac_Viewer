using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DacPac.UI.Converters;

/// <summary>
/// Multi-value converter that reports whether the first value is contained in the
/// collection supplied as the second value. Used to drive a CheckBox's IsChecked
/// state from membership in a selection collection.
/// </summary>
public sealed class ContainsConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not { } item || values[1] is not IEnumerable collection)
            return false;

        foreach (var element in collection)
        {
            if (Equals(element, item))
                return true;
        }

        return false;
    }
}
