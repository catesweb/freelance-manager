using FreelanceManager.Core.Models;

namespace FreelanceManager.Data.Repositories;

public interface IBusinessProfileRepository
{
    Task<BusinessProfile> GetAsync();    // creates a default row if missing
    Task SaveAsync(BusinessProfile profile);
}
