using System;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using Xunit;

public class AttentionBuilderTests
{
    private static readonly DateTime Today = new(2026, 7, 12);

    [Fact]
    public void Overdue_invoices_come_first_oldest_first()
    {
        var invoices = new[]
        {
            new Invoice { Id = 1, Number = "A", Status = InvoiceStatus.Sent, DueDate = Today.AddDays(-3) },
            new Invoice { Id = 2, Number = "B", Status = InvoiceStatus.Sent, DueDate = Today.AddDays(-30) },
            new Invoice { Id = 3, Number = "C", Status = InvoiceStatus.Sent, DueDate = Today.AddDays(2) }, // due soon, not overdue
        };

        var items = AttentionBuilder.Build(Array.Empty<Project>(), invoices, Today);

        Assert.Equal("Invoice B", items[0].Title);   // most overdue first
        Assert.Equal("Overdue", items[0].Kind);
        Assert.Equal("30 days overdue", items[0].Detail);
        Assert.Equal("Invoice A", items[1].Title);
        Assert.Equal("Invoice C", items[2].Title);
        Assert.Equal("Due soon", items[2].Kind);
    }

    [Fact]
    public void Active_projects_due_within_week_included_but_others_excluded()
    {
        var projects = new[]
        {
            new Project { Id = 1, Title = "Soon", Status = ProjectStatus.Active, DueDate = Today.AddDays(5) },
            new Project { Id = 2, Title = "Far", Status = ProjectStatus.Active, DueDate = Today.AddDays(20) },
            new Project { Id = 3, Title = "Done", Status = ProjectStatus.Complete, DueDate = Today.AddDays(2) },
            new Project { Id = 4, Title = "No date", Status = ProjectStatus.Active },
        };

        var items = AttentionBuilder.Build(projects, Array.Empty<Invoice>(), Today);

        var item = Assert.Single(items);
        Assert.Equal("Soon", item.Title);
        Assert.Equal(1, item.ProjectId);
        Assert.Null(item.InvoiceId);
    }

    [Fact]
    public void Stale_leads_appear_last_after_two_weeks()
    {
        var projects = new[]
        {
            new Project { Id = 1, Title = "Old lead", Status = ProjectStatus.Lead, CreatedAt = Today.AddDays(-21) },
            new Project { Id = 2, Title = "Fresh lead", Status = ProjectStatus.Lead, CreatedAt = Today.AddDays(-3) },
        };
        var invoices = new[]
        {
            new Invoice { Id = 1, Number = "X", Status = InvoiceStatus.Sent, DueDate = Today.AddDays(-1) },
        };

        var items = AttentionBuilder.Build(projects, invoices, Today);

        Assert.Equal(2, items.Count);
        Assert.Equal("Overdue", items[0].Kind);
        Assert.Equal("Stale lead", items[1].Kind);
        Assert.Equal("Old lead", items[1].Title);
    }

    [Fact]
    public void Empty_inputs_produce_no_items()
    {
        Assert.Empty(AttentionBuilder.Build(Array.Empty<Project>(), Array.Empty<Invoice>(), Today));
    }

    [Fact]
    public void Paid_and_draft_invoices_are_ignored()
    {
        var invoices = new[]
        {
            new Invoice { Id = 1, Number = "P", Status = InvoiceStatus.Paid, DueDate = Today.AddDays(-10) },
            new Invoice { Id = 2, Number = "D", Status = InvoiceStatus.Draft, DueDate = Today.AddDays(-10) },
        };

        Assert.Empty(AttentionBuilder.Build(Array.Empty<Project>(), invoices, Today));
    }
}
