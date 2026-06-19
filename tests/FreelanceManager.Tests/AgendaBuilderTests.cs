using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

public class AgendaBuilderTests
{
    [Fact]
    public void BuildWeek_includes_only_due_dates_in_current_week_sorted()
    {
        var today = new DateTime(2026, 6, 17); // a Wednesday
        var projects = new[]
        {
            new Project { Title = "In week",  DueDate = new DateTime(2026, 6, 19) },
            new Project { Title = "Next week", DueDate = new DateTime(2026, 6, 25) },
            new Project { Title = "No date",   DueDate = null },
        };
        var invoices = new[]
        {
            new Invoice { Number = "INV-1", DueDate = new DateTime(2026, 6, 16) },
        };

        var items = AgendaBuilder.BuildWeek(projects, invoices, today);

        Assert.Equal(2, items.Count);
        Assert.Equal(new DateTime(2026, 6, 16), items[0].Date); // invoice first (earlier)
        Assert.Equal("Invoice", items[0].Kind);
        Assert.Equal("In week", items[1].Title);
    }
}
