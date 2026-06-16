using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class BusinessProfileRepository : IBusinessProfileRepository
{
    private readonly AppDbContext _db;
    public BusinessProfileRepository(AppDbContext db) => _db = db;

    public async Task<BusinessProfile> GetAsync()
    {
        var profile = await _db.BusinessProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new BusinessProfile { Id = 1 };
            _db.BusinessProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }
        return profile;
    }

    public async Task SaveAsync(BusinessProfile profile)
    {
        _db.BusinessProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }
}
