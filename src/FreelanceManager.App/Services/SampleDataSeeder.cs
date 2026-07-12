using System;
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;

namespace FreelanceManager.App.Services;

/// <summary>
/// Seeds a small set of example records so a fresh install has something to explore.
/// Names carry a "(Sample)" suffix; records are ordinary rows the user deletes normally.
/// </summary>
public class SampleDataSeeder
{
    public const string Marker = "(Sample)";

    private readonly IClientRepository _clients;
    private readonly IProjectRepository _projects;
    private readonly IInvoiceRepository _invoices;
    private readonly IPaymentRepository _payments;
    private readonly IBusinessProfileRepository _profiles;
    private readonly IClock _clock;

    public SampleDataSeeder(IClientRepository clients, IProjectRepository projects,
        IInvoiceRepository invoices, IPaymentRepository payments,
        IBusinessProfileRepository profiles, IClock clock)
    {
        _clients = clients; _projects = projects; _invoices = invoices;
        _payments = payments; _profiles = profiles; _clock = clock;
    }

    /// <summary>Returns false if sample data already exists (nothing seeded).</summary>
    public async Task<bool> SeedAsync()
    {
        if ((await _clients.GetAllAsync()).Any(c => c.Name.Contains(Marker)))
            return false;

        var today = _clock.Today;
        var currency = (await _profiles.GetAsync()).DefaultCurrency;

        var coffee = await _clients.AddAsync(new Client
        {
            Name = $"Harbor Coffee Co. {Marker}",
            Company = "Harbor Coffee Co.",
            Email = "hello@harborcoffee.example",
            Notes = "Sample record — safe to delete."
        });
        var physio = await _clients.AddAsync(new Client
        {
            Name = $"Lakeside Physio {Marker}",
            Company = "Lakeside Physio",
            Email = "reception@lakesidephysio.example",
            Notes = "Sample record — safe to delete."
        });
        var tutoring = await _clients.AddAsync(new Client
        {
            Name = $"Bright Path Tutoring {Marker}",
            Company = "Bright Path Tutoring",
            Email = "info@brightpath.example",
            Notes = "Sample record — safe to delete."
        });

        var redesign = await _projects.AddAsync(new Project
        {
            ClientId = coffee.Id,
            Title = $"Website redesign {Marker}",
            Status = ProjectStatus.Active,
            StartDate = today.AddDays(-20),
            DueDate = today.AddDays(10),
            BuildStackNotes = "Static site, Tailwind, deployed on Netlify."
        });
        var booking = await _projects.AddAsync(new Project
        {
            ClientId = physio.Id,
            Title = $"Booking site {Marker}",
            Status = ProjectStatus.Active,
            StartDate = today.AddDays(-35),
            DueDate = today.AddDays(3)
        });
        await _projects.AddAsync(new Project
        {
            ClientId = tutoring.Id,
            Title = $"Landing page {Marker}",
            Status = ProjectStatus.Lead,
            CreatedAt = today.AddDays(-21)
        });
        var brand = await _projects.AddAsync(new Project
        {
            ClientId = coffee.Id,
            Title = $"Brand refresh {Marker}",
            Status = ProjectStatus.Complete,
            StartDate = today.AddDays(-90),
            DueDate = today.AddDays(-30)
        });

        // sample numbers deliberately avoid the real INV- numbering sequence
        await _invoices.AddAsync(new Invoice
        {
            Number = "SAMPLE-0001", ClientId = coffee.Id, ProjectId = redesign.Id,
            Status = InvoiceStatus.Sent, Currency = currency,
            IssueDate = today.AddDays(-10), DueDate = today.AddDays(4),
            LineItems =
            {
                new InvoiceLineItem { Description = "Design", Quantity = 1, UnitPrice = 1200m },
                new InvoiceLineItem { Description = "Development", Quantity = 1, UnitPrice = 2400m }
            }
        });
        await _invoices.AddAsync(new Invoice
        {
            Number = "SAMPLE-0002", ClientId = physio.Id, ProjectId = booking.Id,
            Status = InvoiceStatus.Sent, Currency = currency,
            IssueDate = today.AddDays(-40), DueDate = today.AddDays(-12),
            LineItems = { new InvoiceLineItem { Description = "Deposit — booking site", Quantity = 1, UnitPrice = 900m } }
        });
        var paid = await _invoices.AddAsync(new Invoice
        {
            Number = "SAMPLE-0003", ClientId = coffee.Id, ProjectId = brand.Id,
            Status = InvoiceStatus.Paid, Currency = currency,
            IssueDate = today.AddDays(-60), DueDate = today.AddDays(-46),
            LineItems = { new InvoiceLineItem { Description = "Brand refresh", Quantity = 1, UnitPrice = 1500m } }
        });
        await _invoices.AddAsync(new Invoice
        {
            Number = "SAMPLE-0004", ClientId = tutoring.Id,
            Status = InvoiceStatus.Draft, Currency = currency,
            IssueDate = today, DueDate = today.AddDays(14),
            LineItems = { new InvoiceLineItem { Description = "Landing page — estimate", Quantity = 1, UnitPrice = 800m } }
        });
        var partPaid = await _invoices.AddAsync(new Invoice
        {
            Number = "SAMPLE-0005", ClientId = physio.Id,
            Status = InvoiceStatus.Sent, Currency = currency,
            IssueDate = today.AddDays(-20), DueDate = today.AddDays(-6),
            LineItems = { new InvoiceLineItem { Description = "Content updates", Quantity = 4, UnitPrice = 150m } }
        });

        await _payments.AddAsync(new Payment
        {
            InvoiceId = paid.Id, Amount = 1500m,
            Date = today.AddDays(-45), Method = "Bank transfer"
        });
        await _payments.AddAsync(new Payment
        {
            InvoiceId = partPaid.Id, Amount = 300m,
            Date = today.AddDays(-3), Method = "PayPal"
        });

        return true;
    }
}
