using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private string _activePage = "Dashboard";

    public bool IsDashboardActive => ActivePage == "Dashboard";
    public bool IsClientsActive   => ActivePage == "Clients";
    public bool IsProjectsActive  => ActivePage == "Projects";
    public bool IsInvoicesActive  => ActivePage == "Invoices";
    public bool IsSettingsActive  => ActivePage == "Settings";

    partial void OnActivePageChanged(string value)
    {
        OnPropertyChanged(nameof(IsDashboardActive));
        OnPropertyChanged(nameof(IsClientsActive));
        OnPropertyChanged(nameof(IsProjectsActive));
        OnPropertyChanged(nameof(IsInvoicesActive));
        OnPropertyChanged(nameof(IsSettingsActive));
    }

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        ShowDashboard();
    }

    [RelayCommand] private void ShowDashboard() { CurrentPage = _services.GetRequiredService<DashboardViewModel>(); ActivePage = "Dashboard"; }
    [RelayCommand] private void ShowClients()   { CurrentPage = _services.GetRequiredService<ClientsViewModel>();   ActivePage = "Clients"; }
    [RelayCommand] private void ShowProjects()  { CurrentPage = _services.GetRequiredService<ProjectsViewModel>();  ActivePage = "Projects"; }
    [RelayCommand] private void ShowInvoices()  { CurrentPage = _services.GetRequiredService<InvoicesViewModel>();  ActivePage = "Invoices"; }
    [RelayCommand] private void ShowSettings()  { CurrentPage = _services.GetRequiredService<SettingsViewModel>();  ActivePage = "Settings"; }

    [RelayCommand] private void QuickNewClient()
    {
        ShowClients();
        (CurrentPage as ClientsViewModel)?.NewCommand.Execute(null);
    }

    [RelayCommand] private void QuickNewProject()
    {
        ShowProjects();
        (CurrentPage as ProjectsViewModel)?.NewCommand.Execute(null);
    }

    [RelayCommand] private void QuickNewInvoice()
    {
        ShowInvoices();
        (CurrentPage as InvoicesViewModel)?.NewCommand.Execute(null);
    }
}
