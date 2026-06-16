using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using FreelanceManager.Core.Models;

namespace FreelanceManager.App.ViewModels;

public partial class ClientEditViewModel : ViewModelBase
{
    private static readonly Regex EmailRx =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public int Id { get; }

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _company;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _notes;

    public ClientEditViewModel(Client model)
    {
        Id = model.Id;
        _name = model.Name;
        _company = model.Company;
        _email = model.Email;
        _phone = model.Phone;
        _address = model.Address;
        _notes = model.Notes;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        (string.IsNullOrWhiteSpace(Email) || EmailRx.IsMatch(Email));

    public void ApplyTo(Client model)
    {
        model.Name = Name.Trim();
        model.Company = Company;
        model.Email = Email;
        model.Phone = Phone;
        model.Address = Address;
        model.Notes = Notes;
    }
}
