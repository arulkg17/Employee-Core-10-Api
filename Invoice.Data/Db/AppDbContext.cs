using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Data.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CustomerEntity> Customers { get; set; }
    public DbSet<VendorEntity> Vendors { get; set; }
    public DbSet<CategoryEntity> Category => Set<CategoryEntity>();
    public DbSet<ItemmasterEntity> Itemmasters => Set<ItemmasterEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductEntity>()
        .Property(p => p.Price)
        .HasPrecision(18, 2);

        // optional seed sample data
        modelBuilder.Entity<ProductEntity>().HasData(
           new ProductEntity { Id = 1, Name = "Sample A", Description = "Sample product A", Price = 9.99m, Stock = 100 },
           new ProductEntity { Id = 2, Name = "Sample B", Description = "Sample product B", Price = 19.99m, Stock = 50 }
        );
        // modelBuilder.Entity<CustomerEntity>().HasData(
        //    new CustomerEntity { Id = 1, Name = "Sample A", Email = "arulkg@gmail.com", Phone = "6035608952" },
        //    new CustomerEntity { Id = 2, Name = "Sample B", Email = "arulkg1@gmail.com", Phone = "6035608953" }
        //);


    }
}