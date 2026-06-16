using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IBusinessProfileRepository _profiles;
    private readonly IBackupService _backup;
    private BusinessProfile _model = new();

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private string _defaultCurrency = "USD";
    [ObservableProperty] private decimal _defaultTaxRate;
    [ObservableProperty] private string _invoiceNumberFormat = "INV-{YYYY}-{0000}";
    [ObservableProperty] private string? _statusMessage;

    public SettingsViewModel(IBusinessProfileRepository profiles, IBackupService backup)
    {
        _profiles = profiles;
        _backup = backup;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _model = await _profiles.GetAsync();
            Name = _model.Name;
            Address = _model.Address;
            Email = _model.Email;
            Phone = _model.Phone;
            LogoPath = _model.LogoPath;
            DefaultCurrency = _model.DefaultCurrency;
            DefaultTaxRate = _model.DefaultTaxRate;
            InvoiceNumberFormat = _model.InvoiceNumberFormat;
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        _model.Name = Name;
        _model.Address = Address;
        _model.Email = Email;
        _model.Phone = Phone;
        _model.LogoPath = LogoPath;
        _model.DefaultCurrency = DefaultCurrency;
        _model.DefaultTaxRate = DefaultTaxRate;
        _model.InvoiceNumberFormat = InvoiceNumberFormat;
        await _profiles.SaveAsync(_model);
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private async Task BackupNow()
    {
        try
        {
            string dest = await _backup.BackupAsync(AppPaths.DatabasePath, AppPaths.DefaultBackupDir);
            StatusMessage = $"Backed up to {dest}";
        }
        catch (System.Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
    }
}
