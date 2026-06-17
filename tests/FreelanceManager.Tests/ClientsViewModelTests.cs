using System.Collections.Generic;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientsViewModelTests
{
    private sealed class FakeRepo : IClientRepository
    {
        public bool ThrowInUse;
        public bool Deleted;
        public Task<List<Client>> GetAllAsync() => Task.FromResult(new List<Client>());
        public Task<Client?> GetAsync(int id) => Task.FromResult<Client?>(new Client { Id = id });
        public Task<Client> AddAsync(Client c) => Task.FromResult(c);
        public Task UpdateAsync(Client c) => Task.CompletedTask;
        public Task DeleteAsync(int id)
        {
            if (ThrowInUse) throw new ClientInUseException("Client has projects or invoices.");
            Deleted = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDialogs : IDialogService
    {
        public bool ConfirmResult = true;
        public Task<bool> ConfirmAsync(string t, string m, string c = "Confirm", string x = "Cancel") => Task.FromResult(ConfirmResult);
        public Task<bool> ShowDialogAsync(object vm) => Task.FromResult(false);
    }

    private sealed class FakeNotes : INotificationService
    {
        public string? LastMessage; public NotificationKind LastKind;
        public void Show(string message, NotificationKind kind = NotificationKind.Info) { LastMessage = message; LastKind = kind; }
    }

    [Fact]
    public async Task Delete_does_nothing_when_not_confirmed()
    {
        var repo = new FakeRepo();
        var vm = new ClientsViewModel(repo, new FakeDialogs { ConfirmResult = false }, new FakeNotes())
                 { Selected = new Client { Id = 1 } };

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.False(repo.Deleted);
    }

    [Fact]
    public async Task Delete_in_use_reports_error_notification()
    {
        var repo = new FakeRepo { ThrowInUse = true };
        var notes = new FakeNotes();
        var vm = new ClientsViewModel(repo, new FakeDialogs { ConfirmResult = true }, notes)
                 { Selected = new Client { Id = 1 } };

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(NotificationKind.Error, notes.LastKind);
        Assert.Contains("projects or invoices", notes.LastMessage);
    }
}
