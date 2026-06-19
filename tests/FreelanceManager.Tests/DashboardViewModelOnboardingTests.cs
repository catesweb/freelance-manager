using System.Collections.Generic;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

/// <summary>
/// Onboarding checklist strip: ShowOnboarding is true on fresh data, false after dismiss.
/// Uses the same file-local stub pattern as DashboardViewModelAgendaTests.
/// </summary>
public class DashboardViewModelOnboardingTests
{
    // ── stubs ────────────────────────────────────────────────────────────────

    private sealed class StubProjectRepo : IProjectRepository
    {
        public Task<List<Project>> GetAllAsync() => Task.FromResult(new List<Project>());
        public Task<List<Project>> GetByClientAsync(int clientId) => Task.FromResult(new List<Project>());
        public Task<Project?> GetAsync(int id) => Task.FromResult<Project?>(null);
        public Task<Project> AddAsync(Project p) => Task.FromResult(p);
        public Task UpdateAsync(Project p) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class StubInvoiceRepo : IInvoiceRepository
    {
        private readonly List<Invoice> _items;
        public StubInvoiceRepo(IEnumerable<Invoice>? items = null)
            => _items = items is null ? new List<Invoice>() : new List<Invoice>(items);
        public Task<List<Invoice>> GetAllAsync() => Task.FromResult(new List<Invoice>(_items));
        public Task<Invoice?> GetAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice> AddAsync(Invoice i) => Task.FromResult(i);
        public Task UpdateAsync(Invoice i) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<int> GetMaxSequenceForYearAsync(int year) => Task.FromResult(0);
    }

    private sealed class StubClientRepo : IClientRepository
    {
        private readonly List<Client> _items;
        public StubClientRepo(IEnumerable<Client>? items = null)
            => _items = items is null ? new List<Client>() : new List<Client>(items);
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>(_items));
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(null);
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class StubProfileRepo : IBusinessProfileRepository
    {
        private readonly BusinessProfile _profile;
        public StubProfileRepo(BusinessProfile? profile = null)
            => _profile = profile ?? new BusinessProfile();
        public Task<BusinessProfile> GetAsync() => Task.FromResult(_profile);
        public Task SaveAsync(BusinessProfile profile) => Task.CompletedTask;
    }

    private sealed class StubAppState : IAppStateService
    {
        public bool OnboardingDismissed { get; private set; }
        public void DismissOnboarding() => OnboardingDismissed = true;
    }

    private sealed class FixedClock : FreelanceManager.Core.Services.IClock
    {
        public System.DateTime Today => new System.DateTime(2026, 1, 1);
    }

    private sealed class SilentNotifications : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    // ── fixture helper ────────────────────────────────────────────────────────

    /// <summary>Fresh VM: empty data, onboarding not dismissed.</summary>
    private static (DashboardViewModel vm, StubAppState appState) Fresh()
    {
        var appState = new StubAppState();
        var vm = new DashboardViewModel(
            new StubProjectRepo(),
            new StubInvoiceRepo(),
            new FixedClock(),
            new SilentNotifications(),
            appState,
            new StubProfileRepo(),
            new StubClientRepo());
        return (vm, appState);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ShowOnboarding_true_on_fresh_empty_data()
    {
        var (vm, _) = Fresh();
        await vm.RefreshAsync();
        Assert.True(vm.ShowOnboarding);
    }

    [Fact]
    public async Task ShowOnboarding_false_after_dismiss()
    {
        var (vm, _) = Fresh();
        await vm.RefreshAsync();
        Assert.True(vm.ShowOnboarding);

        vm.DismissOnboardingCommand.Execute(null);
        Assert.False(vm.ShowOnboarding);
    }

    [Fact]
    public async Task ShowOnboarding_false_when_already_dismissed()
    {
        var appState = new StubAppState();
        appState.DismissOnboarding(); // pre-dismissed
        var vm = new DashboardViewModel(
            new StubProjectRepo(),
            new StubInvoiceRepo(),
            new FixedClock(),
            new SilentNotifications(),
            appState,
            new StubProfileRepo(),
            new StubClientRepo());

        await vm.RefreshAsync();
        Assert.False(vm.ShowOnboarding);
    }

    [Fact]
    public async Task ShowOnboarding_false_when_all_steps_complete()
    {
        var profile = new BusinessProfile { Name = "ACME Corp" };
        var client = new Client { Id = 1, Name = "Client A" };
        var invoice = new Invoice { Id = 1 };

        var vm = new DashboardViewModel(
            new StubProjectRepo(),
            new StubInvoiceRepo(new[] { invoice }),
            new FixedClock(),
            new SilentNotifications(),
            new StubAppState(),
            new StubProfileRepo(profile),
            new StubClientRepo(new[] { client }));

        await vm.RefreshAsync();
        Assert.True(vm.StepProfileDone);
        Assert.True(vm.StepClientDone);
        Assert.True(vm.StepInvoiceDone);
        Assert.False(vm.ShowOnboarding);
    }

    [Fact]
    public async Task StepProfileDone_false_when_name_empty()
    {
        var (vm, _) = Fresh(); // profile has empty Name
        await vm.RefreshAsync();
        Assert.False(vm.StepProfileDone);
    }

    [Fact]
    public async Task StepClientDone_false_when_no_clients()
    {
        var (vm, _) = Fresh();
        await vm.RefreshAsync();
        Assert.False(vm.StepClientDone);
    }

    [Fact]
    public async Task StepInvoiceDone_false_when_no_invoices()
    {
        var (vm, _) = Fresh();
        await vm.RefreshAsync();
        Assert.False(vm.StepInvoiceDone);
    }
}
