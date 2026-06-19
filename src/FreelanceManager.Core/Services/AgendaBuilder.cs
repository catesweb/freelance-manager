namespace FreelanceManager.Core.Services;

using FreelanceManager.Core.Models;

public record AgendaItem(DateTime Date, string Kind, string Title, string? Trailing);

public static class AgendaBuilder
{
    public static IReadOnlyList<AgendaItem> BuildWeek(
        IEnumerable<Project> projects, IEnumerable<Invoice> invoices, DateTime today)
    {
        var monday = today.Date.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var sunday = monday.AddDays(6);
        bool InWeek(DateTime d) => d.Date >= monday && d.Date <= sunday;

        var items = new List<AgendaItem>();
        foreach (var p in projects)
            if (p.DueDate is { } d && InWeek(d))
                items.Add(new AgendaItem(d.Date, "Project", p.Title, "Deadline"));
        foreach (var i in invoices)
            if (InWeek(i.DueDate))
                items.Add(new AgendaItem(i.DueDate.Date, "Invoice", i.Number, "Due"));

        return items.OrderBy(x => x.Date).ThenBy(x => x.Kind).ToList();
    }
}
