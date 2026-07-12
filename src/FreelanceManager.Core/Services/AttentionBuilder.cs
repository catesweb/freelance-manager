namespace FreelanceManager.Core.Services;

using FreelanceManager.Core.Models;

/// <summary>One actionable dashboard item; exactly one of ProjectId/InvoiceId is set.</summary>
public record AttentionItem(string Kind, string Title, string Detail, int? ProjectId, int? InvoiceId);

public static class AttentionBuilder
{
    // ponytail: fixed thresholds; make configurable if anyone asks
    private const int DueSoonDays = 7;
    private const int StaleLeadDays = 14;

    /// <summary>
    /// Overdue invoices (oldest first), then items due within a week, then leads
    /// with no activity since creation for two weeks.
    /// </summary>
    public static IReadOnlyList<AttentionItem> Build(
        IEnumerable<Project> projects, IEnumerable<Invoice> invoices, DateTime today)
    {
        var soon = today.Date.AddDays(DueSoonDays);
        var items = new List<AttentionItem>();

        foreach (var i in invoices.Where(i => OverduePolicy.IsOverdue(i, today))
                                  .OrderBy(i => i.DueDate))
        {
            int days = (today.Date - i.DueDate.Date).Days;
            items.Add(new AttentionItem("Overdue", $"Invoice {i.Number}",
                $"{days} day{(days == 1 ? "" : "s")} overdue", null, i.Id));
        }

        var dueSoon = new List<(DateTime Date, AttentionItem Item)>();
        foreach (var i in invoices)
            if (i.Status == InvoiceStatus.Sent && i.DueDate.Date >= today.Date && i.DueDate.Date <= soon)
                dueSoon.Add((i.DueDate, new AttentionItem("Due soon", $"Invoice {i.Number}",
                    $"Due {i.DueDate:MMM dd}", null, i.Id)));
        foreach (var p in projects)
            if (p.Status == ProjectStatus.Active && p.DueDate is { } d
                && d.Date >= today.Date && d.Date <= soon)
                dueSoon.Add((d, new AttentionItem("Due soon", p.Title, $"Due {d:MMM dd}", p.Id, null)));
        items.AddRange(dueSoon.OrderBy(x => x.Date).Select(x => x.Item));

        foreach (var p in projects.Where(p => p.Status == ProjectStatus.Lead
                                              && p.CreatedAt.Date <= today.Date.AddDays(-StaleLeadDays))
                                  .OrderBy(p => p.CreatedAt))
        {
            int days = (today.Date - p.CreatedAt.Date).Days;
            items.Add(new AttentionItem("Stale lead", p.Title, $"No activity in {days} days", p.Id, null));
        }

        return items;
    }
}
