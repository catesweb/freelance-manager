using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;

namespace FreelanceManager.App.ViewModels;

public partial class InvoiceEditViewModel : ViewModelBase
{
    public int Id { get; }

    [ObservableProperty] private string _number = string.Empty;
    [ObservableProperty] private int _clientId;
    [ObservableProperty] private int? _projectId;
    [ObservableProperty] private System.DateTimeOffset _issueDate = System.DateTimeOffset.Now;
    [ObservableProperty] private System.DateTimeOffset _dueDate = System.DateTimeOffset.Now.AddDays(14);
    [ObservableProperty] private InvoiceStatus _status = InvoiceStatus.Draft;
    [ObservableProperty] private string _currency = "USD";
    [ObservableProperty] private decimal _taxRate;
    [ObservableProperty] private string? _notes;

    public ObservableCollection<LineItemViewModel> Lines { get; } = new();

    public InvoiceStatus[] StatusOptions { get; } =
        (InvoiceStatus[])System.Enum.GetValues(typeof(InvoiceStatus));

    public InvoiceEditViewModel(Invoice model)
    {
        Id = model.Id;
        _number = model.Number;
        _clientId = model.ClientId;
        _projectId = model.ProjectId;
        _issueDate = new System.DateTimeOffset(model.IssueDate);
        _dueDate = new System.DateTimeOffset(model.DueDate);
        _status = model.Status;
        _currency = model.Currency;
        _taxRate = model.TaxRate;
        _notes = model.Notes;

        foreach (var li in model.LineItems) AddLineInternal(new LineItemViewModel(li));
        Lines.CollectionChanged += (_, _) => RecalculateTotals();
    }

    public decimal Subtotal => InvoiceCalculator.Subtotal(ToModel());
    public decimal Tax => InvoiceCalculator.Tax(ToModel());
    public decimal Total => InvoiceCalculator.Total(ToModel());

    partial void OnTaxRateChanged(decimal value) => RecalculateTotals();

    private void AddLineInternal(LineItemViewModel line)
    {
        line.PropertyChanged += OnLinePropertyChanged;
        Lines.Add(line);
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e) => RecalculateTotals();

    private void RecalculateTotals()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Tax));
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand] private void AddLine() => AddLineInternal(new LineItemViewModel());

    [RelayCommand]
    private void RemoveLine(LineItemViewModel? line)
    {
        if (line is null) return;
        line.PropertyChanged -= OnLinePropertyChanged;
        Lines.Remove(line);
        RecalculateTotals();
    }

    public bool IsValid => ClientId > 0 && !string.IsNullOrWhiteSpace(Number);

    public Invoice ToModel() => new()
    {
        Id = Id,
        Number = Number,
        ClientId = ClientId,
        ProjectId = ProjectId,
        IssueDate = IssueDate.DateTime,
        DueDate = DueDate.DateTime,
        Status = Status,
        Currency = Currency,
        TaxRate = TaxRate,
        Notes = Notes,
        LineItems = Lines.Select(l => l.ToModel()).ToList()
    };
}
