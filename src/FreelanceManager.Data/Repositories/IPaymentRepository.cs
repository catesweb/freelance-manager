using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetForInvoiceAsync(int invoiceId);
    Task<decimal> GetTotalPaidAsync(int invoiceId);
    Task AddAsync(Payment payment);
    Task DeleteAsync(int id);
}
