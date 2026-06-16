using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public ProjectRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<Project>> GetAllAsync()
    {
        using var db = _factory.CreateDbContext();
        return await db.Projects.Include(p => p.Client).OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<List<Project>> GetByClientAsync(int clientId)
    {
        using var db = _factory.CreateDbContext();
        return await db.Projects.Where(p => p.ClientId == clientId)
                                .OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Project?> GetAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        return await db.Projects.Include(p => p.Client)
                                .Include(p => p.Invoices)
                                .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> AddAsync(Project project)
    {
        using var db = _factory.CreateDbContext();
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        using var db = _factory.CreateDbContext();
        db.Projects.Update(project);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = _factory.CreateDbContext();
        var p = await db.Projects.FindAsync(id);
        if (p is null) return;
        db.Projects.Remove(p);
        await db.SaveChangesAsync();
    }
}
