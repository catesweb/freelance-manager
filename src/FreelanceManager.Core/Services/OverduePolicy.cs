using FreelanceManager.Core.Models;

namespace FreelanceManager.Core.Services;

public static class OverduePolicy
{
    public static bool IsOverdue(Invoice invoice, DateTime today)
        => invoice.Status == InvoiceStatus.Sent && invoice.DueDate.Date < today.Date;

    public static InvoiceStatus EffectiveStatus(Invoice invoice, DateTime today)
        => IsOverdue(invoice, today) ? InvoiceStatus.Overdue : invoice.Status;
}
