using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace FreelanceManager.App.Converters;

// ponytail: bool -> GridLength for two-state master/detail columns.
// Parameter is "trueLen|falseLen", e.g. "*|0" (detail) or "360|*" (list).
public sealed class BoolToGridLengthConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string ?? "*|0").Split('|');
        return GridLength.Parse(value is true ? parts[0] : parts[1]);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
