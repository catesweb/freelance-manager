using System.Text.RegularExpressions;
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public InvoiceRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Invoice>> GetAllAsync()
    {
        using var db = _factory.CreateDbContext();
        return await db.Invoices.Include(i => i.Client).Include(i => i.Project)
                                .Include(i => i.LineItems)
                                .OrderByDescending(i => i.IssueDate).ToListAsync();
    }

    public async Task<Invoice?> GetAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        return await db.Invoices.Include(i => i.Client).Include(i => i.Project)
                                .Include(i => i.LineItems)
                                .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Invoice> AddAsync(Invoice invoice)
    {
        using var db = _factory.CreateDbContext();
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        using var db = _factory.CreateDbContext();
        // replace line items wholesale to keep edit logic simple and correct
        var existing = await db.Invoices.Include(i => i.LineItems)
                                        .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        if (existing is null) return;

        db.InvoiceLineItems.RemoveRange(existing.LineItems);
        db.Entry(existing).CurrentValues.SetValues(invoice);
        existing.LineItems = invoice.LineItems;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        var inv = await db.Invoices.FindAsync(id);
        if (inv is null) return;
        db.Invoices.Remove(inv);
        await db.SaveChangesAsync();
    }

    public async Task<int> GetMaxSequenceForYearAsync(int year)
    {
        using var db = _factory.CreateDbContext();
        var numbers = await db.Invoices.Select(i => i.Number).ToListAsync();
        int max = 0;
        var rx = new Regex(@"(\d+)\s*$");      // trailing digits
        foreach (var n in numbers)
        {
            if (n.Contains(year.ToString("D4")))
            {
                var m = rx.Match(n);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int seq) && seq > max)
                    max = seq;
            }
        }
        return max;
    }
}
