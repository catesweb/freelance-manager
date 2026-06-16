using System.Text.RegularExpressions;
using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;
    public InvoiceRepository(AppDbContext db) => _db = db;

    public async Task<List<Invoice>> GetAllAsync()
        => await _db.Invoices.Include(i => i.Client).Include(i => i.Project)
                             .Include(i => i.LineItems)
                             .OrderByDescending(i => i.IssueDate).ToListAsync();

    public async Task<Invoice?> GetAsync(int id)
        => await _db.Invoices.Include(i => i.Client).Include(i => i.Project)
                             .Include(i => i.LineItems)
                             .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<Invoice> AddAsync(Invoice invoice)
    {
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        // replace line items wholesale to keep edit logic simple and correct
        var existing = await _db.Invoices.Include(i => i.LineItems)
                                         .FirstOrDefaultAsync(i => i.Id == invoice.Id);
        if (existing is null) return;

        _db.InvoiceLineItems.RemoveRange(existing.LineItems);
        _db.Entry(existing).CurrentValues.SetValues(invoice);
        existing.LineItems = invoice.LineItems;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var inv = await _db.Invoices.FindAsync(id);
        if (inv is null) return;
        _db.Invoices.Remove(inv);
        await _db.SaveChangesAsync();
    }

    public async Task<int> GetMaxSequenceForYearAsync(int year)
    {
        var numbers = await _db.Invoices.Select(i => i.Number).ToListAsync();
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
