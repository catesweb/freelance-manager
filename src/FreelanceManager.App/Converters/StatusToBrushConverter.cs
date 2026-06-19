using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace FreelanceManager.App.Converters;

public class StatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value?.ToString() ?? string.Empty;
        var wantFg = string.Equals(parameter as string, "fg", StringComparison.OrdinalIgnoreCase);

        var (bgKey, fgKey) = name switch
        {
            "Paid" or "Complete" or "Active" => ("SuccessSubtle", "Success"),
            "Overdue" => ("DangerSubtle", "Danger"),
            "Sent" => ("WarningSubtle", "Warning"),
            "Lead" or "Draft" => ("InfoSubtle", "Info"),
            "Archived" => ("InfoSubtle", "Info"),
            _ => ("InfoSubtle", "Info")
        };

        var key = wantFg ? fgKey : bgKey;
        var app = Application.Current;
        if (app is not null)
        {
            var variant = app.ActualThemeVariant;
            if (app.TryFindResource(key, variant, out var res) && res is IBrush brush)
                return brush;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
