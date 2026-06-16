using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceCalculatorTests
{
    private static Invoice MakeInvoice(decimal taxRate, params (decimal qty, decimal price)[] lines)
    {
        var inv = new Invoice { TaxRate = taxRate };
        foreach (var (qty, price) in lines)
            inv.LineItems.Add(new InvoiceLineItem { Quantity = qty, UnitPrice = price });
        return inv;
    }

    [Fact]
    public void Subtotal_sums_line_totals()
    {
        var inv = MakeInvoice(0m, (2m, 50m), (1m, 25m));
        Assert.Equal(125m, InvoiceCalculator.Subtotal(inv));
    }

    [Fact]
    public void Tax_is_subtotal_times_rate_rounded_to_two_places()
    {
        var inv = MakeInvoice(0.2m, (1m, 99.99m));
        Assert.Equal(20.00m, InvoiceCalculator.Tax(inv));
    }

    [Fact]
    public void Total_is_subtotal_plus_tax()
    {
        var inv = MakeInvoice(0.2m, (1m, 100m));
        Assert.Equal(120.00m, InvoiceCalculator.Total(inv));
    }

    [Fact]
    public void Empty_invoice_totals_zero()
    {
        var inv = MakeInvoice(0.2m);
        Assert.Equal(0m, InvoiceCalculator.Subtotal(inv));
        Assert.Equal(0m, InvoiceCalculator.Tax(inv));
        Assert.Equal(0m, InvoiceCalculator.Total(inv));
    }

    [Fact]
    public void Rounding_is_away_from_zero_at_midpoint()
    {
        // subtotal 10.125 * tax 1.0 -> 10.13 (not 10.12)
        var inv = MakeInvoice(1.0m, (1m, 10.125m));
        Assert.Equal(10.13m, InvoiceCalculator.Tax(inv));
    }
}
