using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class InvoiceRepositoryTests
{
    private static async Task<int> SeedClientAsync(TestDb db)
        => (await new ClientRepository(db.CreateFactory()).AddAsync(new Client { Name = "Acme" })).Id;

    [Fact]
    public async Task Add_persists_invoice_with_line_items()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new InvoiceRepository(db.CreateFactory());

        await repo.AddAsync(new Invoice
        {
            ClientId = clientId,
            Number = "INV-2026-0001",
            TaxRate = 0.2m,
            LineItems = { new InvoiceLineItem { Description = "Design", Quantity = 2, UnitPrice = 100 } }
        });

        var saved = (await new InvoiceRepository(db.CreateFactory()).GetAllAsync()).Single();
        Assert.Equal("INV-2026-0001", saved.Number);
        Assert.Single(saved.LineItems);
        Assert.Equal(200m, saved.LineItems[0].LineTotal);
    }

    [Fact]
    public async Task MaxSequenceForYear_is_zero_when_no_invoices()
    {
        using var db = new TestDb();
        var repo = new InvoiceRepository(db.CreateFactory());
        Assert.Equal(0, await repo.GetMaxSequenceForYearAsync(2026));
    }

    [Fact]
    public async Task MaxSequenceForYear_reads_trailing_number_of_matching_year()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new InvoiceRepository(db.CreateFactory());
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2026-0003" });
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2026-0007" });
        await repo.AddAsync(new Invoice { ClientId = clientId, Number = "INV-2025-0099" });

        Assert.Equal(7, await new InvoiceRepository(db.CreateFactory()).GetMaxSequenceForYearAsync(2026));
    }
}
