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
    private readonly IEmailSender _emailSender;
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

    [ObservableProperty] private string? _smtpHost;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string? _smtpUsername;
    [ObservableProperty] private string _smtpPassword = string.Empty;   // entry only; blank keeps existing
    [ObservableProperty] private bool _smtpUseSsl = true;
    [ObservableProperty] private string? _smtpFromEmail;
    [ObservableProperty] private string? _smtpFromName;

    public static ThemeMode[] ThemeOptions { get; } = System.Enum.GetValues<ThemeMode>();

    public string AppVersion => _updates.CurrentVersion;

    public SettingsViewModel(IBusinessProfileRepository profiles, IBackupService backup, IThemeService themeService, INotificationService notes, IUpdateService updates, IEmailSender email)
    {
        _profiles = profiles;
        _backup = backup;
        _themeService = themeService;
        _notes = notes;
        _updates = updates;
        _emailSender = email;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task TestSmtp()
    {
        var probe = new BusinessProfile
        {
            Name = Name, Email = Email,
            SmtpHost = SmtpHost, SmtpPort = SmtpPort,
            SmtpUsername = SmtpUsername,
            SmtpPasswordEncrypted = _model.SmtpPasswordEncrypted,
            SmtpUseSsl = SmtpUseSsl,
            SmtpFromEmail = SmtpFromEmail, SmtpFromName = SmtpFromName
        };
        try
        {
            await _emailSender.TestConnectionAsync(probe, SmtpPassword);
            _notes.Show("SMTP connection succeeded.", NotificationKind.Success);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"SMTP test failed: {ex.Message}", NotificationKind.Error);
        }
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
            SmtpHost = _model.SmtpHost;
            SmtpPort = _model.SmtpPort;
            SmtpUsername = _model.SmtpUsername;
            SmtpUseSsl = _model.SmtpUseSsl;
            SmtpFromEmail = _model.SmtpFromEmail;
            SmtpFromName = _model.SmtpFromName;
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
        _model.SmtpHost = SmtpHost;
        _model.SmtpPort = SmtpPort;
        _model.SmtpUsername = SmtpUsername;
        _model.SmtpUseSsl = SmtpUseSsl;
        _model.SmtpFromEmail = SmtpFromEmail;
        _model.SmtpFromName = SmtpFromName;
        if (!string.IsNullOrEmpty(SmtpPassword))   // blank = keep existing encrypted password
        {
            _model.SmtpPasswordEncrypted = Dpapi.Encrypt(SmtpPassword);
            SmtpPassword = string.Empty;
        }
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
