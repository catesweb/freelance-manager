using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public PaymentRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Payment>> GetForInvoiceAsync(int invoiceId)
    {
        using var db = _factory.CreateDbContext();
        return await db.Payments.Where(p => p.InvoiceId == invoiceId)
                                .OrderByDescending(p => p.Date).ToListAsync();
    }

    public async Task<decimal> GetTotalPaidAsync(int invoiceId)
    {
        using var db = _factory.CreateDbContext();
        return await db.Payments.Where(p => p.InvoiceId == invoiceId)
                                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
    }

    public async Task AddAsync(Payment payment)
    {
        using var db = _factory.CreateDbContext();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        var p = await db.Payments.FindAsync(id);
        if (p is null) return;
        db.Payments.Remove(p);
        await db.SaveChangesAsync();
    }
}
