using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty] private int _activeProjects;
    [ObservableProperty] private int _overdueCount;
    [ObservableProperty] private decimal _outstandingTotal;

    public ObservableCollection<AgendaItem> Agenda { get; } = new();
    public ObservableCollection<Project> PinnedProjects { get; } = new();

    public DashboardViewModel(IProjectRepository projects, IInvoiceRepository invoices, IClock clock, INotificationService notes)
    {
        _projects = projects;
        _invoices = invoices;
        _clock = clock;
        _notes = notes;
        _ = RefreshAsync();
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

            PinnedProjects.Clear();
            foreach (var p in projects.Where(p => p.Status == ProjectStatus.Active)
                                      .OrderByDescending(p => p.CreatedAt).Take(5))
                PinnedProjects.Add(p);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
