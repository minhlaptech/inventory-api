using InventoryApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryApi.Infrastructure;

/// <summary>
/// EF Core DbContext for the Inventory API.
/// Configured for PostgreSQL with global query filters and audit tracking.
/// </summary>
public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ─── Product Configuration ───────────────────
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(20);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
            entity.Property(p => p.CostPrice).HasPrecision(18, 2);
            entity.Property(p => p.Unit).HasMaxLength(20).HasDefaultValue("pcs");
            entity.Property(p => p.ImageUrl).HasMaxLength(500);

            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.CategoryId);
            entity.HasIndex(p => p.IsActive);

            // Computed properties not stored in DB
            entity.Ignore(p => p.IsLowStock);
            entity.Ignore(p => p.ProfitMargin);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasQueryFilter(p => !p.IsDeleted);
        });

        // ─── Category Configuration ──────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.HasIndex(c => c.Slug).IsUnique();
            entity.HasQueryFilter(c => !c.IsDeleted);

            // Seed categories
            entity.HasData(
                new Category { Id = Guid.Parse("c0000001-0000-0000-0000-000000000001"), Name = "Electronics", Slug = "electronics", Description = "Electronic devices and components", SortOrder = 1 },
                new Category { Id = Guid.Parse("c0000002-0000-0000-0000-000000000002"), Name = "Office Supplies", Slug = "office-supplies", Description = "Stationery and office equipment", SortOrder = 2 },
                new Category { Id = Guid.Parse("c0000003-0000-0000-0000-000000000003"), Name = "Raw Materials", Slug = "raw-materials", Description = "Manufacturing inputs and materials", SortOrder = 3 }
            );
        });

        // ─── Supplier Configuration ──────────────────
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.ContactEmail).IsRequired().HasMaxLength(255);
            entity.Property(s => s.ContactPhone).HasMaxLength(20);
            entity.Property(s => s.Address).HasMaxLength(500);
            entity.Property(s => s.Website).HasMaxLength(300);
            entity.HasIndex(s => s.ContactEmail).IsUnique();
            entity.HasQueryFilter(s => !s.IsDeleted);
        });

        // ─── StockMovement Configuration ─────────────
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(sm => sm.Id);
            entity.Property(sm => sm.Type).HasConversion<string>().HasMaxLength(20);
            entity.Property(sm => sm.ReferenceNumber).HasMaxLength(50);
            entity.Property(sm => sm.Notes).HasMaxLength(1000);
            entity.Property(sm => sm.PerformedBy).HasMaxLength(100);

            entity.HasIndex(sm => sm.ProductId);
            entity.HasIndex(sm => sm.CreatedAt);
            entity.HasIndex(sm => sm.Type);

            entity.HasOne(sm => sm.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
