using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync();
    Task<List<Project>> GetByClientAsync(int clientId);
    Task<Project?> GetAsync(int id);
    Task<Project> AddAsync(Project project);
    Task UpdateAsync(Project project);
    Task DeleteAsync(int id);
}
