using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.ViewModels;

public partial class InvoicesViewModel : ViewModelBase
{
    private readonly IInvoiceRepository _invoices;
    private readonly IClientRepository _clients;
    private readonly IProjectRepository _projects;
    private readonly IInvoiceNumberGenerator _numbers;
    private readonly IBusinessProfileRepository _profiles;
    private readonly IPdfExporter _pdf;
    private readonly IClock _clock;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notes;

    /// <summary>Supplied by the View: returns a chosen output path or null if cancelled.</summary>
    public Func<string, Task<string?>>? SavePdfPathProvider { get; set; }

    public ObservableCollection<InvoiceRow> Invoices { get; } = new();
    public ObservableCollection<Client> ClientOptions { get; } = new();
    public ObservableCollection<Project> ProjectOptions { get; } = new();

    [ObservableProperty] private InvoiceRow? _selected;
    [ObservableProperty] private InvoiceEditViewModel? _editor;
    [ObservableProperty] private Client? _editorClient;

    public bool IsEditing => Editor is not null;
    public bool IsNotEditing => Editor is null;
    public bool IsEmpty => Invoices.Count == 0;

    partial void OnEditorChanged(InvoiceEditViewModel? value)
    {
        OnPropertyChanged(nameof(IsEditing));
        OnPropertyChanged(nameof(IsNotEditing));
    }

    public InvoicesViewModel(
        IInvoiceRepository invoices, IClientRepository clients, IProjectRepository projects,
        IInvoiceNumberGenerator numbers, IBusinessProfileRepository profiles,
        IPdfExporter pdf, IClock clock,
        IDialogService dialogs, INotificationService notes)
    {
        _invoices = invoices; _clients = clients; _projects = projects;
        _numbers = numbers; _profiles = profiles; _pdf = pdf; _clock = clock;
        _dialogs = dialogs; _notes = notes;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Invoices.Clear();
            foreach (var i in await _invoices.GetAllAsync())
                Invoices.Add(new InvoiceRow(i, OverduePolicy.EffectiveStatus(i, _clock.Today)));

            ClientOptions.Clear();
            foreach (var c in await _clients.GetAllAsync()) ClientOptions.Add(c);
            ProjectOptions.Clear();
            foreach (var p in await _projects.GetAllAsync()) ProjectOptions.Add(p);

            OnPropertyChanged(nameof(IsEmpty));
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand]
    private async Task New()
    {
        var profile = await _profiles.GetAsync();
        int year = _clock.Today.Year;
        int lastSeq = await _invoices.GetMaxSequenceForYearAsync(year);
        string number = _numbers.Next(profile.InvoiceNumberFormat, year, lastSeq);

        Editor = new InvoiceEditViewModel(new Invoice
        {
            Number = number,
            Currency = profile.DefaultCurrency,
            TaxRate = profile.DefaultTaxRate,
            IssueDate = _clock.Today,
            DueDate = _clock.Today.AddDays(14)
        });
        EditorClient = null;
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Selected is null) return;
        var full = await _invoices.GetAsync(Selected.Id);
        if (full is null) return;
        Editor = new InvoiceEditViewModel(full);
        EditorClient = ClientOptions.FirstOrDefault(c => c.Id == full.ClientId);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Editor is null) return;
        if (EditorClient is not null) Editor.ClientId = EditorClient.Id;
        if (!Editor.IsValid)
        {
            _notes.Show("Client and invoice number are required.", NotificationKind.Error);
            return;
        }

        try
        {
            var model = Editor.ToModel();
            if (model.Id == 0) await _invoices.AddAsync(model);
            else await _invoices.UpdateAsync(model);

            Editor = null;
            _notes.Show("Invoice saved.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Save failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand] private void Cancel() => Editor = null;

    [RelayCommand]
    private async Task Delete()
    {
        if (Selected is null) return;
        if (!await _dialogs.ConfirmAsync("Delete invoice",
                $"Delete invoice {Selected.Number}? This cannot be undone.", "Delete"))
            return;
        try
        {
            await _invoices.DeleteAsync(Selected.Id);
            await LoadAsync();
            _notes.Show("Invoice deleted.", NotificationKind.Success);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Delete failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        if (Selected is null || SavePdfPathProvider is null) return;
        try
        {
            var invoice = await _invoices.GetAsync(Selected.Id);
            if (invoice is null) return;

            string? path = await SavePdfPathProvider($"{invoice.Number}.pdf");
            if (string.IsNullOrWhiteSpace(path)) return;

            var profile = await _profiles.GetAsync();
            _pdf.ExportInvoice(invoice, profile, path);
            _notes.Show($"Exported to {path}", NotificationKind.Success);
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Export failed: {ex.Message}", NotificationKind.Error);
        }
    }
}

public class InvoiceRow
{
    public InvoiceRow(Invoice inv, InvoiceStatus effectiveStatus)
    {
        Id = inv.Id;
        Number = inv.Number;
        ClientName = inv.Client?.Name ?? "";
        IssueDate = inv.IssueDate;
        DueDate = inv.DueDate;
        Status = effectiveStatus;
        Total = InvoiceCalculator.Total(inv);
        Currency = inv.Currency;
    }

    public int Id { get; }
    public string Number { get; }
    public string ClientName { get; }
    public System.DateTime IssueDate { get; }
    public System.DateTime DueDate { get; }
    public InvoiceStatus Status { get; }
    public decimal Total { get; }
    public string Currency { get; }
}
