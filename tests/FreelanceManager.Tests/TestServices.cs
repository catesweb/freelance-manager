using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.Tests;

/// <summary>
/// Builds a minimal DI container for MainWindowViewModel tests.
/// Uses in-memory fakes for all infrastructure — no SQLite, no PDF, no file I/O.
/// </summary>
public static class TestServices
{
    public static IServiceProvider Build()
    {
        var sc = new ServiceCollection();

        // Infrastructure fakes
        sc.AddSingleton<IClock, FakeClock>();
        sc.AddSingleton<IInvoiceNumberGenerator, FakeInvoiceNumberGenerator>();
        sc.AddSingleton<IBackupService, FakeBackupService>();
        sc.AddSingleton<IPdfExporter, FakePdfExporter>();
        sc.AddSingleton<IThemeService, FakeThemeService>();
        sc.AddSingleton<IDialogService, FakeDialogService>();
        sc.AddSingleton<INotificationService, FakeNotificationService>();

        // Repository fakes
        sc.AddTransient<IClientRepository, FakeClientRepository>();
        sc.AddTransient<IProjectRepository, FakeProjectRepository>();
        sc.AddTransient<IInvoiceRepository, FakeInvoiceRepository>();
        sc.AddTransient<IBusinessProfileRepository, FakeBusinessProfileRepository>();

        // ViewModels
        sc.AddTransient<DashboardViewModel>();
        sc.AddTransient<ClientsViewModel>();
        sc.AddTransient<ProjectsViewModel>();
        sc.AddTransient<InvoicesViewModel>();
        sc.AddTransient<SettingsViewModel>();
        sc.AddSingleton<MainWindowViewModel>();

        return sc.BuildServiceProvider();
    }

    // ── Fakes ──────────────────────────────────────────────────────────────

    private sealed class FakeClock : IClock
    {
        public DateTime Today => new DateTime(2026, 1, 1);
    }

    private sealed class FakeInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        public string Next(string format, int year, int lastSeq) => $"INV-{year}-{lastSeq + 1:0000}";
    }

    private sealed class FakeBackupService : IBackupService
    {
        public Task<string> BackupAsync(string sourcePath, string destDir) => Task.FromResult("backup.db");
    }

    private sealed class FakePdfExporter : IPdfExporter
    {
        public void ExportInvoice(Invoice invoice, BusinessProfile profile, string outputPath) { }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public void Apply(ThemeMode mode) { }
    }

    private sealed class FakeDialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message, string confirm = "Confirm", string cancel = "Cancel")
            => Task.FromResult(false);
        public Task<bool> ShowDialogAsync(object vm) => Task.FromResult(false);
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    private sealed class FakeClientRepository : IClientRepository
    {
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(null);
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public Task<List<Project>> GetAllAsync() => Task.FromResult(new List<Project>());
        public Task<List<Project>> GetByClientAsync(int clientId) => Task.FromResult(new List<Project>());
        public Task<Project?> GetAsync(int id) => Task.FromResult<Project?>(null);
        public Task<Project> AddAsync(Project p) => Task.FromResult(p);
        public Task UpdateAsync(Project p) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public Task<List<Invoice>> GetAllAsync() => Task.FromResult(new List<Invoice>());
        public Task<Invoice?> GetAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice> AddAsync(Invoice i) => Task.FromResult(i);
        public Task UpdateAsync(Invoice i) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<int> GetMaxSequenceForYearAsync(int year) => Task.FromResult(0);
    }

    private sealed class FakeBusinessProfileRepository : IBusinessProfileRepository
    {
        public Task<BusinessProfile> GetAsync() => Task.FromResult(new BusinessProfile());
        public Task SaveAsync(BusinessProfile p) => Task.CompletedTask;
    }
}
