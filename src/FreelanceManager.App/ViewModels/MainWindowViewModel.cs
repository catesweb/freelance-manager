using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace FreelanceManager.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    public MainWindowViewModel(IServiceProvider services)
    {
        _services = services;
        ShowDashboard();
    }

    [RelayCommand] private void ShowDashboard() => CurrentPage = _services.GetRequiredService<DashboardViewModel>();
    [RelayCommand] private void ShowClients()   => CurrentPage = _services.GetRequiredService<ClientsViewModel>();
    [RelayCommand] private void ShowProjects()  => CurrentPage = _services.GetRequiredService<ProjectsViewModel>();
    [RelayCommand] private void ShowInvoices()  => CurrentPage = _services.GetRequiredService<InvoicesViewModel>();
    [RelayCommand] private void ShowSettings()  => CurrentPage = _services.GetRequiredService<SettingsViewModel>();
}
