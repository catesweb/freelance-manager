using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace FreelanceManager.App.Converters;

/// <summary>Formats a date as yyyy-MM-dd; blank for null or uninitialized (MinValue) dates.</summary>
public class OptionalDateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is DateTime d && d > DateTime.MinValue ? d.ToString("yyyy-MM-dd") : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
