using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly IProjectRepository _projects;
    private readonly IClientRepository _clients;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<Client> ClientOptions { get; } = new();

    [ObservableProperty] private Project? _selected;
    [ObservableProperty] private ProjectEditViewModel? _editor;
    [ObservableProperty] private Client? _editorClient;
    [ObservableProperty] private string? _statusMessage;

    public ProjectsViewModel(IProjectRepository projects, IClientRepository clients)
    {
        _projects = projects;
        _clients = clients;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Projects.Clear();
        foreach (var p in await _projects.GetAllAsync()) Projects.Add(p);
        ClientOptions.Clear();
        foreach (var c in await _clients.GetAllAsync()) ClientOptions.Add(c);
    }

    [RelayCommand] private void New()
    {
        Editor = new ProjectEditViewModel(new Project());
        EditorClient = null;
    }

    [RelayCommand] private void Edit()
    {
        if (Selected is null) return;
        Editor = new ProjectEditViewModel(Selected);
        EditorClient = null;
        foreach (var c in ClientOptions) if (c.Id == Selected.ClientId) EditorClient = c;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null) return;
        if (EditorClient is not null) Editor.ClientId = EditorClient.Id;
        if (!Editor.IsValid) { StatusMessage = "Title and client are required."; return; }

        if (Editor.Id == 0)
        {
            var model = new Project();
            Editor.ApplyTo(model);
            await _projects.AddAsync(model);
        }
        else
        {
            var model = await _projects.GetAsync(Editor.Id);
            if (model is not null) { Editor.ApplyTo(model); await _projects.UpdateAsync(model); }
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
        await _projects.DeleteAsync(Selected.Id);
        await LoadAsync();
        StatusMessage = "Deleted.";
    }
}
