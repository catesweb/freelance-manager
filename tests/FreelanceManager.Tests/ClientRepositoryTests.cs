using System.Linq;
using System.Threading.Tasks;
using FreelanceManager.Core.Models;
using FreelanceManager.Data;
using FreelanceManager.Data.Repositories;
using Xunit;

namespace FreelanceManager.Tests;

public class ClientRepositoryTests
{
    [Fact]
    public async Task Add_then_GetAll_returns_the_client()
    {
        using var db = new TestDb();
        var repo = new ClientRepository(db.CreateFactory());

        await repo.AddAsync(new Client { Name = "Acme" });

        var repo2 = new ClientRepository(db.CreateFactory());
        var all = await repo2.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Acme", all[0].Name);
    }

    [Fact]
    public async Task Delete_client_without_dependents_succeeds()
    {
        using var db = new TestDb();
        var repo = new ClientRepository(db.CreateFactory());
        var c = await repo.AddAsync(new Client { Name = "Temp" });

        await new ClientRepository(db.CreateFactory()).DeleteAsync(c.Id);

        var all = await new ClientRepository(db.CreateFactory()).GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task Delete_client_with_project_throws_ClientInUse()
    {
        using var db = new TestDb();
        var c = await new ClientRepository(db.CreateFactory()).AddAsync(new Client { Name = "Busy" });

        await using (var ctx = db.NewContext())
        {
            ctx.Projects.Add(new Project { ClientId = c.Id, Title = "Site" });
            await ctx.SaveChangesAsync();
        }

        var repo = new ClientRepository(db.CreateFactory());
        await Assert.ThrowsAsync<ClientInUseException>(() => repo.DeleteAsync(c.Id));
    }

    [Fact]
    public async Task Delete_client_with_invoice_throws_ClientInUse()
    {
        using var db = new TestDb();
        var c = await new ClientRepository(db.CreateFactory()).AddAsync(new Client { Name = "Billed" });

        await using (var ctx = db.NewContext())
        {
            ctx.Invoices.Add(new Invoice { ClientId = c.Id, Number = "INV-1" });
            await ctx.SaveChangesAsync();
        }

        var repo = new ClientRepository(db.CreateFactory());
        await Assert.ThrowsAsync<ClientInUseException>(() => repo.DeleteAsync(c.Id));
    }
}
