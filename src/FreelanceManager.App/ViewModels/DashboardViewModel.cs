using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IProjectRepository _projects;
    private readonly IInvoiceRepository _invoices;
    private readonly IClock _clock;
    private readonly INotificationService _notes;
    private readonly IAppStateService _appState;
    private readonly IBusinessProfileRepository _profiles;
    private readonly IClientRepository _clients;

    [ObservableProperty] private int _activeProjects;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private decimal _outstandingTotal;

    [ObservableProperty] private bool _showOnboarding;
    [ObservableProperty] private bool _stepProfileDone;
    [ObservableProperty] private bool _stepClientDone;
    [ObservableProperty] private bool _stepInvoiceDone;

    public ObservableCollection<AgendaItem> Agenda { get; } = new();
    public ObservableCollection<Project> PinnedProjects { get; } = new();
    public ObservableCollection<AttentionItem> Attention { get; } = new();

    public bool HasAttention => Attention.Count > 0;

    [RelayCommand]
    private static void OpenAttention(AttentionItem? item)
    {
        if (item is null) return;
        if (item.InvoiceId is { } inv)
            WeakReferenceMessenger.Default.Send(new OpenInvoiceMessage(inv));
        else if (item.ProjectId is { } proj)
            WeakReferenceMessenger.Default.Send(new OpenProjectMessage(proj));
    }

    [RelayCommand]
    private static void OpenProject(Project? project)
    {
        if (project is not null)
            WeakReferenceMessenger.Default.Send(new OpenProjectMessage(project.Id));
    }

    [RelayCommand]
    private static void OpenPage(string page)
        => WeakReferenceMessenger.Default.Send(new OpenPageMessage(page));

    private readonly SampleDataSeeder _seeder;

    public DashboardViewModel(
        IProjectRepository projects,
        IInvoiceRepository invoices,
        IClock clock,
        INotificationService notes,
        IAppStateService appState,
        IBusinessProfileRepository profiles,
        IClientRepository clients,
        SampleDataSeeder seeder)
    {
        _projects = projects;
        _invoices = invoices;
        _clock = clock;
        _notes = notes;
        _appState = appState;
        _profiles = profiles;
        _clients = clients;
        _seeder = seeder;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task LoadSampleData()
    {
        try
        {
            if (await _seeder.SeedAsync())
            {
                _notes.Show("Sample data added. Names end in (Sample); delete them like any record.", NotificationKind.Success);
                await RefreshAsync();
            }
            else
            {
                _notes.Show("Sample data is already loaded.", NotificationKind.Error);
            }
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Could not load sample data: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand]
    private void DismissOnboarding()
    {
        _appState.DismissOnboarding();
        ShowOnboarding = false;
    }

    public async Task RefreshAsync()
    {
        try
        {
            var projects = (await _projects.GetAllAsync()).ToList();
            var invoices = (await _invoices.GetAllAsync()).ToList();

            ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);

            decimal outstanding = 0m; int overdue = 0;
            foreach (var i in invoices)
            {
                var eff = OverduePolicy.EffectiveStatus(i, _clock.Today);
                if (eff == InvoiceStatus.Overdue) overdue++;
                if (eff is InvoiceStatus.Sent or InvoiceStatus.Overdue)
                    outstanding += InvoiceCalculator.Total(i);
            }
            OverdueCount = overdue;
            OutstandingTotal = outstanding;

            Agenda.Clear();
            foreach (var item in AgendaBuilder.BuildWeek(projects, invoices, _clock.Today))
                Agenda.Add(item);

            Attention.Clear();
            foreach (var item in AttentionBuilder.Build(projects, invoices, _clock.Today))
                Attention.Add(item);
            OnPropertyChanged(nameof(HasAttention));

            PinnedProjects.Clear();
            foreach (var p in projects.Where(p => p.Status == ProjectStatus.Active)
                                      .OrderByDescending(p => p.CreatedAt).Take(5))
                PinnedProjects.Add(p);

            // onboarding step completion
            var profile = await _profiles.GetAsync();
            StepProfileDone = !string.IsNullOrWhiteSpace(profile.Name);
            StepClientDone  = (await _clients.GetAllAsync()).Any();
            StepInvoiceDone = invoices.Count > 0;
            ShowOnboarding  = !_appState.OnboardingDismissed
                              && !(StepProfileDone && StepClientDone && StepInvoiceDone);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
