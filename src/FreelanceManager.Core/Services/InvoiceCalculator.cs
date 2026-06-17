using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public static class InvoiceCalculator
{
    public static decimal Subtotal(Invoice invoice)
    {
        decimal sum = 0m;
        foreach (var item in invoice.LineItems)
            sum += item.LineTotal;
        return Round(sum);
    }

    public static decimal Tax(Invoice invoice)
        => Round(Subtotal(invoice) * invoice.TaxRate);

    public static decimal Total(Invoice invoice)
        => Subtotal(invoice) + Tax(invoice);

    private static decimal Round(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
