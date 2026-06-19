using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class DashboardViewModelAgendaTests
{
    private sealed class StubProjectRepo : IProjectRepository
    {
        private readonly List<Project> _items;
        public StubProjectRepo(IEnumerable<Project> items) => _items = items.ToList();
        public Task<List<Project>> GetAllAsync() => Task.FromResult(_items.ToList());
        public Task<List<Project>> GetByClientAsync(int clientId) => Task.FromResult(new List<Project>());
        public Task<Project?> GetAsync(int id) => Task.FromResult<Project?>(null);
        public Task<Project> AddAsync(Project p) => Task.FromResult(p);
        public Task UpdateAsync(Project p) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class StubInvoiceRepo : IInvoiceRepository
    {
        public Task<List<Invoice>> GetAllAsync() => Task.FromResult(new List<Invoice>());
        public Task<Invoice?> GetAsync(int id) => Task.FromResult<Invoice?>(null);
        public Task<Invoice> AddAsync(Invoice i) => Task.FromResult(i);
        public Task UpdateAsync(Invoice i) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<int> GetMaxSequenceForYearAsync(int year) => Task.FromResult(0);
    }

    private sealed class FixedClock : IClock
    {
        public DateTime Today => new DateTime(2026, 1, 1);
    }

    private sealed class SilentNotifications : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    private sealed class StubAppState : IAppStateService
    {
        public bool OnboardingDismissed => false;
        public void DismissOnboarding() { }
    }

    private sealed class StubProfileRepo : IBusinessProfileRepository
    {
        public Task<BusinessProfile> GetAsync() => Task.FromResult(new BusinessProfile());
        public Task SaveAsync(BusinessProfile profile) => Task.CompletedTask;
    }

    private sealed class StubClientRepo : IClientRepository
    {
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(null);
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private static DashboardViewModel BuildVm(IEnumerable<Project> projects)
        => new DashboardViewModel(
            new StubProjectRepo(projects),
            new StubInvoiceRepo(),
            new FixedClock(),
            new SilentNotifications(),
            new StubAppState(),
            new StubProfileRepo(),
            new StubClientRepo());

    [Fact]
    public async Task Pinned_shows_active_newest_first_capped_at_five()
    {
        var projects = Enumerable.Range(1, 7).Select(n => new Project
        {
            Id = n,
            Title = $"P{n}",
            Status = ProjectStatus.Active,
            CreatedAt = new DateTime(2026, 1, n)
        }).ToArray();

        var vm = BuildVm(projects);
        await vm.RefreshAsync();

        Assert.Equal(5, vm.PinnedProjects.Count);
        Assert.Equal("P7", vm.PinnedProjects[0].Title); // newest first
        Assert.Equal("P6", vm.PinnedProjects[1].Title);
        Assert.Equal("P3", vm.PinnedProjects[4].Title); // oldest of the top 5
    }

    [Fact]
    public async Task Pinned_excludes_non_active_projects()
    {
        var projects = new[]
        {
            new Project { Id = 1, Title = "Active",   Status = ProjectStatus.Active,   CreatedAt = new DateTime(2026, 1, 1) },
            new Project { Id = 2, Title = "Complete", Status = ProjectStatus.Complete, CreatedAt = new DateTime(2026, 1, 2) },
            new Project { Id = 3, Title = "Archived", Status = ProjectStatus.Archived, CreatedAt = new DateTime(2026, 1, 3) },
        };

        var vm = BuildVm(projects);
        await vm.RefreshAsync();

        Assert.Single(vm.PinnedProjects);
        Assert.Equal("Active", vm.PinnedProjects[0].Title);
    }

    [Fact]
    public async Task Agenda_is_populated_from_AgendaBuilder()
    {
        // today is 2026-01-01 (Thursday), week Mon 2025-12-29 .. Sun 2026-01-04
        var projects = new[]
        {
            new Project { Id = 1, Title = "Due this week", Status = ProjectStatus.Active,
                          DueDate = new DateTime(2026, 1, 2), CreatedAt = new DateTime(2026, 1, 1) },
        };

        var vm = BuildVm(projects);
        await vm.RefreshAsync();

        Assert.Contains(vm.Agenda, a => a.Title == "Due this week");
    }
}
