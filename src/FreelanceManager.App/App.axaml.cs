using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FreelanceManager.App.ViewModels;
using FreelanceManager.App.Views;
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
    }
}