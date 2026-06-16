using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

namespace FreelanceManager.Tests;

public class OverduePolicyTests
{
    private static readonly DateTime Today = new(2026, 6, 16);

    private static Invoice Inv(InvoiceStatus status, DateTime due)
        => new() { Status = status, DueDate = due };

    [Fact]
    public void Sent_and_past_due_is_overdue()
        => Assert.True(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Sent, Today.AddDays(-1)), Today));

    [Fact]
    public void Sent_and_due_today_is_not_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Sent, Today), Today));

    [Fact]
    public void Paid_is_never_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Paid, Today.AddDays(-30)), Today));

    [Fact]
    public void Draft_is_never_overdue()
        => Assert.False(OverduePolicy.IsOverdue(Inv(InvoiceStatus.Draft, Today.AddDays(-30)), Today));

    [Fact]
    public void EffectiveStatus_reports_Overdue_for_overdue_invoice()
        => Assert.Equal(InvoiceStatus.Overdue,
            OverduePolicy.EffectiveStatus(Inv(InvoiceStatus.Sent, Today.AddDays(-5)), Today));

    [Fact]
    public void EffectiveStatus_passes_through_when_not_overdue()
        => Assert.Equal(InvoiceStatus.Paid,
            OverduePolicy.EffectiveStatus(Inv(InvoiceStatus.Paid, Today.AddDays(-5)), Today));
}
