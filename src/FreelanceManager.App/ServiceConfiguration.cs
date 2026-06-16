using FreelanceManager.App.Pdf;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Services;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App;

public static class ServiceConfiguration
{
    public static ServiceProvider Build()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<AppDbContext>(o =>
            o.UseSqlite($"Data Source={AppPaths.DatabasePath}"));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPdfExporter, QuestPdfInvoiceExporter>();
        services.AddSingleton<IThemeService, ThemeService>();

        services.AddTransient<IClientRepository, ClientRepository>();
        services.AddTransient<IProjectRepository, ProjectRepository>();
        services.AddTransient<IInvoiceRepository, InvoiceRepository>();
        services.AddTransient<IBusinessProfileRepository, BusinessProfileRepository>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ClientsViewModel>();
        services.AddTransient<ProjectsViewModel>();
        services.AddTransient<InvoicesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        // apply migrations on startup
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var db = factory.CreateDbContext())
        {
            db.Database.Migrate();
        }

        return provider;
    }
}
