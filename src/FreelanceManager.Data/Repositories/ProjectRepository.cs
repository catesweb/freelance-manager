using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;
    public ProjectRepository(AppDbContext db) => _db = db;

    public async Task<List<Project>> GetAllAsync()
        => await _db.Projects.Include(p => p.Client).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<List<Project>> GetByClientAsync(int clientId)
        => await _db.Projects.Where(p => p.ClientId == clientId)
                             .OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<Project?> GetAsync(int id)
        => await _db.Projects.Include(p => p.Client)
                             .Include(p => p.Invoices)
                             .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> AddAsync(Project project)
    {
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(Project project)
    {
        _db.Projects.Update(project);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p is null) return;
        _db.Projects.Remove(p);
        await _db.SaveChangesAsync();
    }
}
