using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IInvoiceRepository
{
    Task<List<Invoice>> GetAllAsync();
    Task<Invoice?> GetAsync(int id);
    Task<Invoice> AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(int id);

    /// <summary>Highest trailing sequence among invoice numbers ending in -NNNN for the given year.</summary>
    Task<int> GetMaxSequenceForYearAsync(int year);
}
