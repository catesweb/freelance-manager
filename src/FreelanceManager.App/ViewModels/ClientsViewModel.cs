using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class ClientsViewModel : ViewModelBase
{
    private readonly IClientRepository _repo;

    public ObservableCollection<Client> Clients { get; } = new();

    [ObservableProperty] private Client? _selected;
    [ObservableProperty] private ClientEditViewModel? _editor;
    [ObservableProperty] private string? _statusMessage;

    public ClientsViewModel(IClientRepository repo)
    {
        _repo = repo;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Clients.Clear();
        foreach (var c in await _repo.GetAllAsync()) Clients.Add(c);
    }

    [RelayCommand] private void New() => Editor = new ClientEditViewModel(new Client());

    [RelayCommand]
    private void Edit()
    {
        if (Selected is not null) Editor = new ClientEditViewModel(Selected);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null || !Editor.IsValid)
        {
            StatusMessage = "Name is required and email must be valid.";
            return;
        }

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
        StatusMessage = "Saved.";
        await LoadAsync();
    }

    [RelayCommand] private void Cancel() => Editor = null;

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        try
        {
            await _repo.DeleteAsync(Selected.Id);
            await LoadAsync();
            StatusMessage = "Deleted.";
        }
        catch (ClientInUseException ex)
        {
            StatusMessage = ex.Message;
        }
    }
}
