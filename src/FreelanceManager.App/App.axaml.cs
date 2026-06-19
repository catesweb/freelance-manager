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
        RegisterEmbeddedFonts();

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

        // Silent background check; notifies only if an update is waiting.
        _ = Services.GetRequiredService<IUpdateService>().CheckOnStartupAsync();
    }

    // Lato (QuestPDF's default font) is embedded as a resource rather than shipped as
    // loose .ttf files, so register it with QuestPDF before any PDF is generated.
    private static void RegisterEmbeddedFonts()
    {
        var asm = typeof(App).Assembly;
        foreach (var name in asm.GetManifestResourceNames())
        {
            if (name.Contains(".Fonts.", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                using var stream = asm.GetManifestResourceStream(name)!;
                QuestPDF.Drawing.FontManager.RegisterFont(stream);
            }
        }
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