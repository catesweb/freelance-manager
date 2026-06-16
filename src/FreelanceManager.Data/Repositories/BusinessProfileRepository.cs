using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data.Repositories;

public class BusinessProfileRepository : IBusinessProfileRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public BusinessProfileRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<BusinessProfile> GetAsync()
    {
        using var db = _factory.CreateDbContext();
        var profile = await db.BusinessProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new BusinessProfile { Id = 1 };
            db.BusinessProfiles.Add(profile);
            await db.SaveChangesAsync();
        }
        return profile;
    }

    public async Task SaveAsync(BusinessProfile profile)
    {
        using var db = _factory.CreateDbContext();
        db.BusinessProfiles.Update(profile);
        await db.SaveChangesAsync();
    }
}
