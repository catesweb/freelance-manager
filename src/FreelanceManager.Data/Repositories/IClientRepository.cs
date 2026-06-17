using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IClientRepository
{
    Task<List<Client>> GetAllAsync();
    Task<Client?> GetAsync(int id);
    Task<Client> AddAsync(Client client);
    Task UpdateAsync(Client client);
    Task DeleteAsync(int id);   // throws ClientInUseException if dependents exist
}
