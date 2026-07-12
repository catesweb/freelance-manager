using System;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

public class ReminderEmailTests
{
    private static readonly DateTime Today = new(2026, 7, 12);

    private static Invoice MakeInvoice(DateTime due) => new()
    {
        Number = "INV-2026-0007",
        Client = new Client { Name = "Acme" },
        Currency = "USD",
        DueDate = due,
        LineItems = { new InvoiceLineItem { Description = "Work", Quantity = 1, UnitPrice = 1000m } }
    };

    [Fact]
    public void Subject_names_invoice_and_business()
    {
        var subject = ReminderEmail.Subject(MakeInvoice(Today), "CATESWEB");
        Assert.Equal("Payment reminder: Invoice INV-2026-0007 from CATESWEB", subject);
    }

    [Fact]
    public void Body_shows_days_overdue_and_balance_net_of_payments()
    {
        var body = ReminderEmail.Body(MakeInvoice(Today.AddDays(-6)), 300m, "CATESWEB", Today);

        Assert.Contains("6 days overdue", body);
        Assert.Contains("USD 700.00", body);
        Assert.Contains("Hi Acme", body);
    }

    [Fact]
    public void Body_for_not_yet_due_invoice_has_no_overdue_wording()
    {
        var body = ReminderEmail.Body(MakeInvoice(Today.AddDays(3)), 0m, "CATESWEB", Today);

        Assert.DoesNotContain("overdue", body);
        Assert.Contains("is due 2026-07-15", body);
        Assert.Contains("USD 1000.00", body);
    }
}
