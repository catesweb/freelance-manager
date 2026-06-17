using Avalonia;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public class ThemeService : IThemeService
{
    public void Apply(ThemeMode mode)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = ThemeVariantMap.Resolve(mode);
    }
}
