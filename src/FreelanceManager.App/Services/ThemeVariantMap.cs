using Avalonia.Styling;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public static class ThemeVariantMap
{
    public static ThemeVariant Resolve(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ThemeVariant.Light,
        ThemeMode.Dark => ThemeVariant.Dark,
        _ => ThemeVariant.Default
    };
}
