using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class BusinessProfileRepositoryTests
{
    [Fact]
    public async Task Get_creates_default_profile_when_none_exists()
    {
        using var db = new TestDb();
        var profile = await new BusinessProfileRepository(db.CreateFactory()).GetAsync();
        Assert.NotNull(profile);
        Assert.Equal("USD", profile.DefaultCurrency);
    }

    [Fact]
    public async Task Save_then_Get_round_trips_changes()
    {
        using var db = new TestDb();
        var repo = new BusinessProfileRepository(db.CreateFactory());
        var p = await repo.GetAsync();
        p.Name = "Christian Design Co";
        p.DefaultTaxRate = 0.2m;
        await new BusinessProfileRepository(db.CreateFactory()).SaveAsync(p);

        var reloaded = await new BusinessProfileRepository(db.CreateFactory()).GetAsync();
        Assert.Equal("Christian Design Co", reloaded.Name);
        Assert.Equal(0.2m, reloaded.DefaultTaxRate);
    }

    [Fact]
    public async Task SaveAsync_persists_theme_preference()
    {
        using var db = new TestDb();
        var repo = new BusinessProfileRepository(db.CreateFactory());
        var profile = await repo.GetAsync();
        profile.Theme = ThemeMode.Dark;

        await new BusinessProfileRepository(db.CreateFactory()).SaveAsync(profile);
        var reloaded = await new BusinessProfileRepository(db.CreateFactory()).GetAsync();

        Assert.Equal(ThemeMode.Dark, reloaded.Theme);
    }
}
