using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class ClientsViewModel : ViewModelBase
{
    private readonly IClientRepository _repo;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notes;

    public ObservableCollection<Client> Clients { get; } = new();

    [ObservableProperty] private Client? _selected;

    public bool IsEmpty => Clients.Count == 0;

    public ClientsViewModel(IClientRepository repo, IDialogService dialogs, INotificationService notes)
    {
        _repo = repo;
        _dialogs = dialogs;
        _notes = notes;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Clients.Clear();
            foreach (var c in await _repo.GetAllAsync()) Clients.Add(c);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task New()
    {
        var editor = new ClientEditViewModel(new Client());
        if (await _dialogs.ShowDialogAsync(editor))
        {
            var model = new Client();
            editor.ApplyTo(model);
            await _repo.AddAsync(model);
            _notes.Show("Client added.", NotificationKind.Success);
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Selected is null) return;
        var editor = new ClientEditViewModel(Selected);
        if (await _dialogs.ShowDialogAsync(editor))
        {
            var model = await _repo.GetAsync(editor.Id);
            if (model is not null)
            {
                editor.ApplyTo(model);
                await _repo.UpdateAsync(model);
                _notes.Show("Client saved.", NotificationKind.Success);
            }
            else
            {
                _notes.Show("Client no longer exists.", NotificationKind.Error);
            }
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        if (!await _dialogs.ConfirmAsync("Delete client",
                $"Delete \"{Selected.Name}\"? This cannot be undone.", "Delete"))
            return;
        try
        {
            await _repo.DeleteAsync(Selected.Id);
            await LoadAsync();
            _notes.Show("Client deleted.", NotificationKind.Success);
        }
        catch (ClientInUseException ex)
        {
            _notes.Show(ex.Message, NotificationKind.Error);
        }
    }
}
