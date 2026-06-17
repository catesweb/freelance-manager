using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class LineItemViewModel : ViewModelBase
{
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private decimal _quantity = 1m;
    [ObservableProperty] private decimal _unitPrice;

    public decimal LineTotal => Quantity * UnitPrice;

    public LineItemViewModel() { }

    public LineItemViewModel(InvoiceLineItem model)
    {
        _description = model.Description;
        _quantity = model.Quantity;
        _unitPrice = model.UnitPrice;
    }

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));

    public InvoiceLineItem ToModel() => new()
    {
        Description = Description,
        Quantity = Quantity,
        UnitPrice = UnitPrice
    };
}
