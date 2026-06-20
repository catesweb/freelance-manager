using FreelanceManager.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelanceManager.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Client>(e =>
        {
            e.HasMany(c => c.Projects).WithOne(p => p.Client!)
                .HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Invoices).WithOne(i => i.Client!)
                .HasForeignKey(i => i.ClientId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Project>(e =>
        {
            e.HasMany(p => p.Invoices).WithOne(i => i.Project!)
                .HasForeignKey(i => i.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Invoice>(e =>
        {
            e.HasMany(i => i.LineItems).WithOne()
                .HasForeignKey(li => li.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            e.Property(i => i.TaxRate).HasPrecision(9, 4);
        });

        b.Entity<InvoiceLineItem>(e =>
        {
            e.Ignore(li => li.LineTotal);   // computed, not persisted
            e.Property(li => li.Quantity).HasPrecision(18, 4);
            e.Property(li => li.UnitPrice).HasPrecision(18, 4);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasPrecision(18, 4);
            e.HasOne<Invoice>().WithMany().HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<BusinessProfile>(e =>
        {
            e.Property(p => p.DefaultTaxRate).HasPrecision(9, 4);
        });
    }
}
