using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public ClientRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Client>> GetAllAsync()
    {
        using var db = _factory.CreateDbContext();
        return await db.Clients.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Client?> GetAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        return await db.Clients.FindAsync(id);
    }

    public async Task<Client> AddAsync(Client client)
    {
        using var db = _factory.CreateDbContext();
        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    public async Task UpdateAsync(Client client)
    {
        using var db = _factory.CreateDbContext();
        db.Clients.Update(client);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        bool hasProjects = await db.Projects.AnyAsync(p => p.ClientId == id);
        bool hasInvoices = await db.Invoices.AnyAsync(i => i.ClientId == id);
        if (hasProjects || hasInvoices)
            throw new ClientInUseException(
                "This client has projects or invoices and cannot be deleted.");

        var client = await db.Clients.FindAsync(id);
        if (client is null) return;
        db.Clients.Remove(client);
        await db.SaveChangesAsync();
    }
}
