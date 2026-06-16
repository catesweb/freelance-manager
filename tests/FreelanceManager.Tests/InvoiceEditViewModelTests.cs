using System.Linq;
using FreelanceManager.App.ViewModels;
using FreelanceManager.Core.Models;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceEditViewModelTests
{
    private static InvoiceEditViewModel NewVm()
        => new(new Invoice { TaxRate = 0.2m, Currency = "USD" });

    [Fact]
    public void Totals_start_at_zero()
    {
        var vm = NewVm();
        Assert.Equal(0m, vm.Subtotal);
        Assert.Equal(0m, vm.Total);
    }

    [Fact]
    public void Adding_a_line_updates_subtotal_and_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Description = "Design";
        vm.Lines[0].Quantity = 2m;
        vm.Lines[0].UnitPrice = 100m;

        Assert.Equal(200m, vm.Subtotal);
        Assert.Equal(40m, vm.Tax);
        Assert.Equal(240m, vm.Total);
    }

    [Fact]
    public void Editing_a_line_quantity_recomputes_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].UnitPrice = 50m;
        vm.Lines[0].Quantity = 1m;
        Assert.Equal(60m, vm.Total);

        vm.Lines[0].Quantity = 3m;
        Assert.Equal(180m, vm.Total);
    }

    [Fact]
    public void Removing_a_line_recomputes_total()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Quantity = 1m; vm.Lines[0].UnitPrice = 100m;
        var line = vm.Lines[0];
        vm.RemoveLineCommand.Execute(line);
        Assert.Equal(0m, vm.Total);
    }

    [Fact]
    public void ToModel_includes_lines_and_taxrate()
    {
        var vm = NewVm();
        vm.AddLineCommand.Execute(null);
        vm.Lines[0].Description = "Dev"; vm.Lines[0].Quantity = 1m; vm.Lines[0].UnitPrice = 500m;

        var model = vm.ToModel();
        Assert.Single(model.LineItems);
        Assert.Equal("Dev", model.LineItems.First().Description);
        Assert.Equal(0.2m, model.TaxRate);
    }
}
