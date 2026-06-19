using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IBusinessProfileRepository _profiles;
    private readonly IBackupService _backup;
    private readonly IThemeService _themeService;
    private readonly INotificationService _notes;
    private readonly IUpdateService _updates;
    private BusinessProfile _model = new();

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _logoPath;
    [ObservableProperty] private string _defaultCurrency = "USD";
    [ObservableProperty] private decimal _defaultTaxRate;
    [ObservableProperty] private string _invoiceNumberFormat = "INV-{YYYY}-{0000}";
    [ObservableProperty] private ThemeMode _theme;

    public static ThemeMode[] ThemeOptions { get; } = System.Enum.GetValues<ThemeMode>();

    public string AppVersion => _updates.CurrentVersion;

    public SettingsViewModel(IBusinessProfileRepository profiles, IBackupService backup, IThemeService themeService, INotificationService notes, IUpdateService updates)
    {
        _profiles = profiles;
        _backup = backup;
        _themeService = themeService;
        _notes = notes;
        _updates = updates;
        _ = LoadAsync();
    }

    [RelayCommand]
    private Task CheckForUpdates() => _updates.CheckAndInstallAsync();

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
            Theme = _model.Theme;
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
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
        _model.Theme = Theme;
        await _profiles.SaveAsync(_model);
        _themeService.Apply(Theme);
        _notes.Show("Settings saved.", NotificationKind.Success);
    }

    [RelayCommand]
    private async Task BackupNow()
    {
        try
        {
            string dest = await _backup.BackupAsync(AppPaths.DatabasePath, AppPaths.DefaultBackupDir);
            _notes.Show($"Backed up to {dest}", NotificationKind.Success);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Backup failed: {ex.Message}", NotificationKind.Error);
        }
    }
}
