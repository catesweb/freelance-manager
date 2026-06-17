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

    public DashboardViewModel(IProjectRepository projects, IInvoiceRepository invoices, IClock clock, INotificationService notes)
    {
        _projects = projects;
        _invoices = invoices;
        _clock = clock;
        _notes = notes;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var projects = await _projects.GetAllAsync();
            ActiveProjects = projects.Count(p => p.Status == ProjectStatus.Active);

            var invoices = await _invoices.GetAllAsync();
            decimal outstanding = 0m;
            int overdue = 0;
            foreach (var i in invoices)
            {
                var eff = OverduePolicy.EffectiveStatus(i, _clock.Today);
                if (eff == InvoiceStatus.Overdue) overdue++;
                if (eff is InvoiceStatus.Sent or InvoiceStatus.Overdue)
                    outstanding += InvoiceCalculator.Total(i);
            }
            OverdueCount = overdue;
            OutstandingTotal = outstanding;
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
