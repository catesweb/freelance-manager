using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;
    public ClientRepository(AppDbContext db) => _db = db;

    public async Task<List<Client>> GetAllAsync()
        => await _db.Clients.OrderBy(c => c.Name).ToListAsync();

    public async Task<Client?> GetAsync(int id)
        => await _db.Clients.FindAsync(id);

    public async Task<Client> AddAsync(Client client)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return client;
    }

    public async Task UpdateAsync(Client client)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        bool hasProjects = await _db.Projects.AnyAsync(p => p.ClientId == id);
        bool hasInvoices = await _db.Invoices.AnyAsync(i => i.ClientId == id);
        if (hasProjects || hasInvoices)
            throw new ClientInUseException(
                "This client has projects or invoices and cannot be deleted.");

        var client = await _db.Clients.FindAsync(id);
        if (client is null) return;
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
    }
}
