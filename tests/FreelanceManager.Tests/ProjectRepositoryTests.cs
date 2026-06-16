using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ProjectRepositoryTests
{
    private static async Task<int> SeedClientAsync(TestDb db)
    {
        var c = await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "Acme" });
        return c.Id;
    }

    [Fact]
    public async Task Add_persists_all_handover_fields()
    {
        using var db = new TestDb();
        int clientId = await SeedClientAsync(db);
        var repo = new ProjectRepository(db.NewContext());

        await repo.AddAsync(new Project
        {
            ClientId = clientId,
            Title = "Marketing site",
            Status = ProjectStatus.Active,
            RepoUrl = "https://github.com/acme/site",
            LiveSiteUrl = "https://acme.com",
            HostingNotes = "Netlify",
            CredentialsLocation = "1Password vault 'Acme'",
            BuildStackNotes = "Astro + Tailwind",
            GeneralNotes = "Launch June"
        });

        var saved = (await new ProjectRepository(db.NewContext()).GetAllAsync()).Single();
        Assert.Equal("Marketing site", saved.Title);
        Assert.Equal(ProjectStatus.Active, saved.Status);
        Assert.Equal("Netlify", saved.HostingNotes);
        Assert.Equal("1Password vault 'Acme'", saved.CredentialsLocation);
        Assert.Equal("Astro + Tailwind", saved.BuildStackNotes);
    }

    [Fact]
    public async Task GetByClient_filters_to_that_client()
    {
        using var db = new TestDb();
        int a = await SeedClientAsync(db);
        int b = (await new ClientRepository(db.NewContext()).AddAsync(new Client { Name = "B" })).Id;
        var repo = new ProjectRepository(db.NewContext());
        await repo.AddAsync(new Project { ClientId = a, Title = "A1" });
        await repo.AddAsync(new Project { ClientId = b, Title = "B1" });

        var forA = await new ProjectRepository(db.NewContext()).GetByClientAsync(a);
        Assert.Single(forA);
        Assert.Equal("A1", forA[0].Title);
    }
}
