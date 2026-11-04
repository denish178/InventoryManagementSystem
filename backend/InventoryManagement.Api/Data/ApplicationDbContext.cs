using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(500);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
            entity.Property(s => s.ContactPerson).HasMaxLength(100);
            entity.Property(s => s.Email).HasMaxLength(150);
            entity.Property(s => s.Phone).HasMaxLength(30);
            entity.Property(s => s.Address).HasMaxLength(300);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.SKU).HasMaxLength(50).IsRequired();
            entity.Property(p => p.Name).HasMaxLength(150).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(500);
            entity.Property(p => p.UnitPrice).HasPrecision(18, 2);
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.HasIndex(p => p.Name);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Products_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                table.HasCheckConstraint("CK_Products_Quantity_NonNegative", "[QuantityInStock] >= 0");
                table.HasCheckConstraint("CK_Products_ReorderLevel_NonNegative", "[ReorderLevel] >= 0");
            });
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.Property(t => t.TransactionType).HasMaxLength(20).IsRequired();
            entity.Property(t => t.ReferenceNumber).HasMaxLength(100);
            entity.Property(t => t.Notes).HasMaxLength(500);
            entity.Property(t => t.CreatedBy).HasMaxLength(256);
            entity.HasIndex(t => t.CreatedAt);
            entity.HasIndex(t => new { t.ProductId, t.CreatedAt });
            entity.HasIndex(t => t.TransactionType);

            entity.HasOne(t => t.Product)
                .WithMany()
                .HasForeignKey(t => t.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_StockTransactions_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint(
                    "CK_StockTransactions_Type",
                    "[TransactionType] IN ('StockIn', 'StockOut')");
            });
        });
    }
}
