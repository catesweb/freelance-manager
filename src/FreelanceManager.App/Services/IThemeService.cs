using FreelanceManager.Core.Models;

namespace FreelanceManager.App.Services;

public interface IThemeService
{
    void Apply(ThemeMode mode);
}
