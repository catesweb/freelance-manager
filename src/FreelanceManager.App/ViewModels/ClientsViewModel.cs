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
    [ObservableProperty] private ClientEditViewModel? _editor;

    public bool IsEditing => Editor is not null;
    public bool IsNotEditing => Editor is null;
    public bool IsEmpty => Clients.Count == 0;

    partial void OnEditorChanged(ClientEditViewModel? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsNotEditing));
    }

    partial void OnSelectedChanged(Client? value)
    {
        if (value is not null) OpenEditor(value);
    }

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

    private void OpenEditor(Client model)
    {
        Editor = new ClientEditViewModel(model);
    }

    [RelayCommand]
    private void New()
    {
        Selected = null;
        Editor = new ClientEditViewModel(new Client());
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null) return;
        if (!Editor.IsValid) { _notes.Show("Name is required.", NotificationKind.Error); return; }

        try
        {
            if (Editor.Id == 0)
            {
                var model = new Client();
                Editor.ApplyTo(model);
                await _repo.AddAsync(model);
            }
            else
            {
                var model = await _repo.GetAsync(Editor.Id);
                if (model is not null) { Editor.ApplyTo(model); await _repo.UpdateAsync(model); }
            }

            Editor = null;
            Selected = null;
            _notes.Show("Client saved.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Save failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Editor = null;
        Selected = null;
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
            Editor = null;
            Selected = null;
            await LoadAsync();
            _notes.Show("Client deleted.", NotificationKind.Success);
        }
        catch (ClientInUseException ex)
        {
            _notes.Show(ex.Message, NotificationKind.Error);
        }
    }
}
