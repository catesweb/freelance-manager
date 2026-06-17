using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.App.Views;
using FreelanceManager.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        Services = ServiceConfiguration.Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();

        // Apply the saved theme without blocking the UI thread during startup.
        // ThemeVariant.Default already follows the OS, so the brief window before this
        // completes shows the system variant rather than a wrong one.
        _ = ApplySavedThemeAsync();
    }

    private static async Task ApplySavedThemeAsync()
    {
        try
        {
            var profiles = Services.GetRequiredService<IBusinessProfileRepository>();
            var theme = Services.GetRequiredService<IThemeService>();
            var profile = await profiles.GetAsync();
            theme.Apply(profile.Theme);
        }
        catch
        {
            // fall back to default variant if the profile can't be read
        }
    }
}