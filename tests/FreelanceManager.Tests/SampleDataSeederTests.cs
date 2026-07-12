using System;
using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.App.Services;
using FreelanceManager.Core.Services;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class SampleDataSeederTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime Today => new(2026, 7, 12);
    }

    private static SampleDataSeeder CreateSeeder(TestDb db)
    {
        var factory = db.CreateFactory();
        return new SampleDataSeeder(
            new ClientRepository(factory),
            new ProjectRepository(factory),
            new InvoiceRepository(factory),
            new PaymentRepository(factory),
            new BusinessProfileRepository(factory),
            new FixedClock());
    }

    [Fact]
    public async Task Seed_creates_marked_records_across_all_entities()
    {
        using var db = new TestDb();
        var seeder = CreateSeeder(db);

        Assert.True(await seeder.SeedAsync());

        using var ctx = db.NewContext();
        Assert.Equal(3, ctx.Clients.Count());
        Assert.Equal(4, ctx.Projects.Count());
        Assert.Equal(5, ctx.Invoices.Count());
        Assert.Equal(2, ctx.Payments.Count());
        Assert.All(ctx.Clients, c => Assert.Contains(SampleDataSeeder.Marker, c.Name));
        Assert.All(ctx.Projects, p => Assert.Contains(SampleDataSeeder.Marker, p.Title));
        Assert.All(ctx.Invoices, i => Assert.StartsWith("SAMPLE-", i.Number));
    }

    [Fact]
    public async Task Seed_is_refused_when_sample_data_already_present()
    {
        using var db = new TestDb();
        var seeder = CreateSeeder(db);

        Assert.True(await seeder.SeedAsync());
        Assert.False(await seeder.SeedAsync());

        using var ctx = db.NewContext();
        Assert.Equal(3, ctx.Clients.Count());   // unchanged — no duplicates
    }
}
