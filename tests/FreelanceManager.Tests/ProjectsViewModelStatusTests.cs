using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ProjectsViewModelStatusTests
{
    private sealed class FakeProjectRepo : IProjectRepository
    {
        public readonly List<Project> Store = new();
        public Task<List<Project>> GetAllAsync() => Task.FromResult(Store.ToList());
        public Task<List<Project>> GetByClientAsync(int clientId) => Task.FromResult(Store.Where(p => p.ClientId == clientId).ToList());
        public Task<Project?> GetAsync(int id) => Task.FromResult(Store.FirstOrDefault(p => p.Id == id));
        public Task<Project> AddAsync(Project p) { Store.Add(p); return Task.FromResult(p); }
        public Task UpdateAsync(Project p) { var i = Store.FindIndex(x => x.Id == p.Id); if (i >= 0) Store[i] = p; return Task.CompletedTask; }
        public Task DeleteAsync(int id) { Store.RemoveAll(p => p.Id == id); return Task.CompletedTask; }
    }

    private sealed class FakeClientRepo : IClientRepository
    {
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(null);
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeDialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string t, string m, string c = "Confirm", string x = "Cancel") => Task.FromResult(true);
        public Task<bool> ShowDialogAsync(object vm) => Task.FromResult(false);
    }

    private sealed class FakeNotes : INotificationService
    {
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { }
    }

    [Fact]
    public async Task SetStatus_persists_chosen_status()
    {
        var repo = new FakeProjectRepo();
        repo.Store.Add(new Project { Id = 1, Title = "Site", Status = ProjectStatus.Lead });
        var vm = new ProjectsViewModel(repo, new FakeClientRepo(), new FakeDialogs(), new FakeNotes())
                 { Selected = repo.Store[0] };

        await vm.SetStatusCommand.ExecuteAsync(ProjectStatus.Active);

        Assert.Equal(ProjectStatus.Active, (await repo.GetAsync(1))!.Status);
    }

    [Fact]
    public async Task SetStatus_noop_when_nothing_selected()
    {
        var repo = new FakeProjectRepo();
        repo.Store.Add(new Project { Id = 1, Status = ProjectStatus.Lead });
        var vm = new ProjectsViewModel(repo, new FakeClientRepo(), new FakeDialogs(), new FakeNotes());

        await vm.SetStatusCommand.ExecuteAsync(ProjectStatus.Active);

        Assert.Equal(ProjectStatus.Lead, repo.Store[0].Status);
    }
}
