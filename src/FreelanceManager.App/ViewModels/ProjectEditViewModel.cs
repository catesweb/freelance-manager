using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class ProjectEditViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty] private int _clientId;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private ProjectStatus _status = ProjectStatus.Lead;
    [ObservableProperty] private string? _repoUrl;
    [ObservableProperty] private string? _liveSiteUrl;
    [ObservableProperty] private string? _hostingNotes;
    [ObservableProperty] private string? _credentialsLocation;
    [ObservableProperty] private string? _buildStackNotes;
    [ObservableProperty] private string? _generalNotes;
    [ObservableProperty] private System.DateTimeOffset? _startDate;
    [ObservableProperty] private System.DateTimeOffset? _dueDate;

    public ProjectStatus[] StatusOptions { get; } =
        (ProjectStatus[])System.Enum.GetValues(typeof(ProjectStatus));

    public ProjectEditViewModel(Project model)
    {
        Id = model.Id;
        _clientId = model.ClientId;
        _title = model.Title;
        _status = model.Status;
        _repoUrl = model.RepoUrl;
        _liveSiteUrl = model.LiveSiteUrl;
        _hostingNotes = model.HostingNotes;
        _credentialsLocation = model.CredentialsLocation;
        _buildStackNotes = model.BuildStackNotes;
        _generalNotes = model.GeneralNotes;
        _startDate = model.StartDate is null ? null : new System.DateTimeOffset(model.StartDate.Value);
        _dueDate = model.DueDate is null ? null : new System.DateTimeOffset(model.DueDate.Value);
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(Title) && ClientId > 0;

    public void ApplyTo(Project model)
    {
        model.ClientId = ClientId;
        model.Title = Title.Trim();
        model.Status = Status;
        model.RepoUrl = RepoUrl;
        model.LiveSiteUrl = LiveSiteUrl;
        model.HostingNotes = HostingNotes;
        model.CredentialsLocation = CredentialsLocation;
        model.BuildStackNotes = BuildStackNotes;
        model.GeneralNotes = GeneralNotes;
        model.StartDate = StartDate?.DateTime;
        model.DueDate = DueDate?.DateTime;
    }
}
