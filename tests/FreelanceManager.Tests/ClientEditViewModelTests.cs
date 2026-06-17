using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientEditViewModelTests
{
    [Fact]
    public void Is_invalid_when_name_is_blank()
    {
        var vm = new ClientEditViewModel(new Client());
        vm.Name = "   ";
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void Is_invalid_when_email_is_malformed()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "not-an-email" };
        Assert.False(vm.IsValid);
    }

    [Fact]
    public void Is_valid_with_name_and_blank_email()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "" };
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void Is_valid_with_name_and_good_email()
    {
        var vm = new ClientEditViewModel(new Client()) { Name = "Acme", Email = "hi@acme.com" };
        Assert.True(vm.IsValid);
    }

    [Fact]
    public void ToModel_copies_fields_back()
    {
        var model = new Client();
        var vm = new ClientEditViewModel(model) { Name = "Acme", Company = "Acme Inc", Email = "hi@acme.com" };
        vm.ApplyTo(model);
        Assert.Equal("Acme", model.Name);
        Assert.Equal("Acme Inc", model.Company);
        Assert.Equal("hi@acme.com", model.Email);
    }
}
