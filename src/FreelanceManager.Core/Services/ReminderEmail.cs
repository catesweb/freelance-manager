namespace FreelanceManager.Core.Services;

using FreelanceManager.Core.Models;

/// <summary>Composes the payment-reminder subject/body for a sent or overdue invoice.</summary>
public static class ReminderEmail
{
    public static string Subject(Invoice invoice, string businessName)
        => $"Payment reminder: Invoice {invoice.Number} from {businessName}";

    public static string Body(Invoice invoice, decimal amountPaid, string businessName, DateTime today)
    {
        decimal balance = InvoiceCalculator.Total(invoice) - amountPaid;
        int daysOverdue = (today.Date - invoice.DueDate.Date).Days;
        string when = daysOverdue > 0
            ? $"was due {invoice.DueDate:yyyy-MM-dd} ({daysOverdue} day{(daysOverdue == 1 ? "" : "s")} overdue)"
            : $"is due {invoice.DueDate:yyyy-MM-dd}";

        return $"Hi {invoice.Client?.Name},\n\n" +
               $"This is a friendly reminder that invoice {invoice.Number} {when}.\n" +
               $"Outstanding balance: {invoice.Currency} {balance:0.00}.\n\n" +
               $"The invoice is attached for reference. If you've already sent payment, please disregard this message.\n\n" +
               $"Thank you,\n{businessName}";
    }
}
