using System.Collections.ObjectModel;
using Avalonia.Collections;
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

    /// <summary>Filterable, sortable view over <see cref="Invoices"/> bound by the DataGrid.</summary>
    public DataGridCollectionView InvoicesView { get; }

    [ObservableProperty] private string _searchText = string.Empty;

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

    partial void OnSelectedChanged(InvoiceRow? value)
    {
        if (value is not null) _ = Edit();
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
        InvoicesView = new DataGridCollectionView(Invoices) { Filter = MatchesSearch };
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => InvoicesView.Refresh();

    private bool MatchesSearch(object item)
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return true;
        if (item is not InvoiceRow row) return false;
        return Contains(row.Number) || Contains(row.ClientName);
    }

    private bool Contains(string? field)
        => field?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true;

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
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Load failed: {ex.Message}", NotificationKind.Error);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    private async Task New()
    {
        Selected = null;
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
            Selected = null;
            _notes.Show("Invoice saved.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Save failed: {ex.Message}", NotificationKind.Error);
        }
    }

    [RelayCommand] private void Cancel()
    {
        Editor = null;
        Selected = null;
    }

    [RelayCommand]
    private async Task SetStatus(InvoiceStatus status)
    {
        if (Selected is null) return;
        try
        {
            var model = await _invoices.GetAsync(Selected.Id);
            if (model is null) return;
            model.Status = status;
            await _invoices.UpdateAsync(model);
            _notes.Show("Status updated.", NotificationKind.Success);
            await LoadAsync();
        }
        catch (System.Exception ex)
        {
            _notes.Show($"Update failed: {ex.Message}", NotificationKind.Error);
        }
    }

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
            Editor = null;
            Selected = null;
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
