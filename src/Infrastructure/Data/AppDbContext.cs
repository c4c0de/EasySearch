using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<InventoryListing> InventoryListings => Set<InventoryListing>();
    public DbSet<SocialAccount> SocialAccounts => Set<SocialAccount>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Dealer>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name).IsRequired().HasMaxLength(100);
            e.Property(d => d.DealerCode).IsRequired().HasMaxLength(50);
            e.HasIndex(d => d.DealerCode).IsUnique();
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Make).IsRequired().HasMaxLength(150);
            e.Property(v => v.Notes).HasMaxLength(500);
            e.HasOne(v => v.Dealer).WithMany(d => d.Vehicles).HasForeignKey(v => v.DealerId);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.Property(c => c.Slug).IsRequired().HasMaxLength(200);
            e.HasOne(c => c.ParentCategory)
             .WithMany(c => c.SubCategories)
             .HasForeignKey(c => c.ParentCategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryListing>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Title).HasMaxLength(150);
            e.Property(l => l.ImageUrl).HasMaxLength(500);
            e.Property(l => l.Description).HasMaxLength(1000);
            e.Property(l => l.Price).HasPrecision(18, 2);
            e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
            // NO ACTION on the dealer link — avoids SQL Server's "multiple cascade paths" error,
            // since Dealer also reaches a listing via Vehicle. Dealers aren't deleted in this app.
            e.HasOne(l => l.Dealer).WithMany().HasForeignKey(l => l.DealerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Vehicle).WithMany(v => v.Parts).HasForeignKey(l => l.VehicleId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.Category).WithMany(c => c.Parts).HasForeignKey(l => l.CategoryId);
            // Per-part contact accounts. NO ACTION (Restrict) on both — SQL Server forbids two
            // SET NULL FKs to the same table. The repository nulls these out before deleting an
            // account, so parts still fall back to the default.
            e.HasOne(l => l.WhatsAppAccount).WithMany().HasForeignKey(l => l.WhatsAppAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.InstagramAccount).WithMany().HasForeignKey(l => l.InstagramAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SocialAccount>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(s => s.Label).IsRequired().HasMaxLength(80);
            e.Property(s => s.Value).IsRequired().HasMaxLength(120);
            // Restrict on the dealer link keeps SQL Server's "multiple cascade paths" rule satisfied.
            e.HasOne(s => s.Dealer).WithMany().HasForeignKey(s => s.DealerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SiteSetting>(e =>
        {
            e.HasKey(s => s.Key);
            e.Property(s => s.Key).HasMaxLength(100);
            e.Property(s => s.Value).IsRequired();
        });

        modelBuilder.Entity<ContentPage>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Slug).IsRequired().HasMaxLength(50);
            e.HasIndex(p => p.Slug).IsUnique();
            e.Property(p => p.Title).IsRequired().HasMaxLength(150);
            e.Property(p => p.Markdown).IsRequired();
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Name).IsRequired().HasMaxLength(100);
            e.Property(b => b.Address).IsRequired().HasMaxLength(400);
            e.Property(b => b.Phone).HasMaxLength(30);
            e.Property(b => b.Email).HasMaxLength(150);
            e.Property(b => b.Hours).HasMaxLength(120);
            e.Property(b => b.MapsUrl).HasMaxLength(500);
            e.HasOne(b => b.Dealer).WithMany().HasForeignKey(b => b.DealerId).OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var dealerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var exteriorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var headlightId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var tailLightId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var engineId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        modelBuilder.Entity<Dealer>().HasData(new Dealer
        {
            Id = dealerId,
            Name = "AutoNex Thrissur",
            DealerCode = "ANT001"
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = exteriorId, Name = "Exterior", Slug = "exterior" },
            new Category { Id = headlightId, Name = "Headlight", Slug = "headlight", ParentCategoryId = exteriorId },
            new Category { Id = tailLightId, Name = "Tail Light", Slug = "tail-light", ParentCategoryId = exteriorId },
            new Category { Id = engineId, Name = "Engine", Slug = "engine" }
        );

        // Seed the existing WhatsApp number as the default so current parts keep a working button.
        modelBuilder.Entity<SocialAccount>().HasData(new SocialAccount
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            DealerId = dealerId,
            Type = Domain.Enums.SocialAccountType.WhatsApp,
            Label = "Main WhatsApp",
            Value = "916238744855",
            IsDefault = true
        });
    }
}
