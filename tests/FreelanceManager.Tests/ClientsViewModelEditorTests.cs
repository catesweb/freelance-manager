using System.Collections.Generic;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientsViewModelEditorTests
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
    public void New_opens_blank_editor()
    {
        var vm = new ClientsViewModel(new FakeRepo(), new FakeDialogs(), new FakeNotes());

        vm.NewCommand.Execute(null);

        Assert.NotNull(vm.Editor);
        Assert.Equal(0, vm.Editor!.Id);
        Assert.True(vm.IsEditing);
        Assert.False(vm.IsNotEditing);
    }

    [Fact]
    public void Cancel_closes_editor()
    {
        var vm = new ClientsViewModel(new FakeRepo(), new FakeDialogs(), new FakeNotes());
        vm.NewCommand.Execute(null);
        Assert.NotNull(vm.Editor);

        vm.CancelCommand.Execute(null);

        Assert.Null(vm.Editor);
        Assert.False(vm.IsEditing);
        Assert.True(vm.IsNotEditing);
    }
}
