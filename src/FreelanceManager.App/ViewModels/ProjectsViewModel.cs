using System.Collections.ObjectModel;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using FreelanceManager.App.Services;

namespace FreelanceManager.App.ViewModels;

public partial class ProjectsViewModel : ViewModelBase
{
    private readonly IProjectRepository _projects;
    private readonly IClientRepository _clients;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notes;

    public ObservableCollection<Project> Projects { get; } = new();
    public ObservableCollection<Client> ClientOptions { get; } = new();

    /// <summary>Filterable, sortable view over <see cref="Projects"/> bound by the DataGrid.</summary>
    public DataGridCollectionView ProjectsView { get; }

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private Project? _selected;
    [ObservableProperty] private ProjectEditViewModel? _editor;
    [ObservableProperty] private Client? _editorClient;

    public bool IsEditing => Editor is not null;
    public bool IsNotEditing => Editor is null;
    public bool IsEmpty => Projects.Count == 0;

    partial void OnEditorChanged(ProjectEditViewModel? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsNotEditing));
    }

    partial void OnSelectedChanged(Project? value)
    {
        if (value is not null) Edit();
    }

    public ProjectsViewModel(IProjectRepository projects, IClientRepository clients, IDialogService dialogs, INotificationService notes)
    {
        _projects = projects;
        _clients = clients;
        _dialogs = dialogs;
        _notes = notes;
        ProjectsView = new DataGridCollectionView(Projects) { Filter = MatchesSearch };
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => ProjectsView.Refresh();

    private bool MatchesSearch(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (item is not Project p) return false;
        var clientName = ClientOptions.FirstOrDefault(c => c.Id == p.ClientId)?.Name;
        return Contains(p.Title) || Contains(clientName);
    }

    private bool Contains(string? field)
        => field?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true;

    private async Task LoadAsync()
    {
        try
        {
            Projects.Clear();
            foreach (var p in await _projects.GetAllAsync()) Projects.Add(p);
            ClientOptions.Clear();
            foreach (var c in await _clients.GetAllAsync()) ClientOptions.Add(c);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand] private void New()
    {
        Selected = null;
        Editor = new ProjectEditViewModel(new Project());
        EditorClient = null;
    }

    private void Edit()
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
        if (!Editor.IsValid) { _notes.Show("Title and client are required.", NotificationKind.Error); return; }

        try
        {
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
            Selected = null;
            _notes.Show("Project saved.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Save failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand] private void Cancel()
    {
        Editor = null;
        Selected = null;
    }

    [RelayCommand]
    private async Task SetStatus(ProjectStatus status)
    {
        if (Selected is null) return;
        try
        {
            var model = await _projects.GetAsync(Selected.Id);
            if (model is null) return;
            model.Status = status;
            await _projects.UpdateAsync(model);
            _notes.Show("Status updated.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Update failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        if (!await _dialogs.ConfirmAsync("Delete project",
                $"Delete \"{Selected.Title}\"? This cannot be undone.", "Delete"))
            return;
        try
        {
            await _projects.DeleteAsync(Selected.Id);
            Editor = null;
            Selected = null;
            await LoadAsync();
            _notes.Show("Project deleted.", NotificationKind.Success);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Delete failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
