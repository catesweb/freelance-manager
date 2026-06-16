using FreelanceManager.App.Pdf;
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

        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlite($"Data Source={AppPaths.DatabasePath}"),
            ServiceLifetime.Transient);

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IInvoiceNumberGenerator, InvoiceNumberGenerator>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPdfExporter, QuestPdfInvoiceExporter>();

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
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }

        return provider;
    }
}
