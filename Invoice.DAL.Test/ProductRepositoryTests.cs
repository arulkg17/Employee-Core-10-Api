using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

public class ProductRepositoryTests
{
    private const string ConnectionString =
        "Server=LAPTOP-BIG8QIRC,1435;" +
        "Database=Accounts_Test;" +
        "User Id=sa;" +
        "Password=123456;" +
        "Encrypt=False;" +
        "TrustServerCertificate=True";

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        // ------------------------------------------------------------
        // Cleanup previous Product test data.
        // ------------------------------------------------------------

        var testProducts = await context.Products
            .Where(x =>
                x.Name == "TEST_PRODUCT_001" ||
                x.Name == "TEST_PRODUCT_002" ||
                x.Name == "TEST_PRODUCT_003")
            .ToListAsync();

        if (testProducts.Count > 0)
        {
            context.Products.RemoveRange(testProducts);
            await context.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // Create test products.
        // ------------------------------------------------------------

        context.Products.AddRange(
            new ProductEntity
            {
                Name = "TEST_PRODUCT_001",
                Price = 100.00m,
                Stock = 10
            },
            new ProductEntity
            {
                Name = "TEST_PRODUCT_002",
                Price = 250.50m,
                Stock = 20
            },
            new ProductEntity
            {
                Name = "TEST_PRODUCT_003",
                Price = 500.75m,
                Stock = 0
            });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestProducts()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);

        var testProducts = result
            .Where(x =>
                x.Name == "TEST_PRODUCT_001" ||
                x.Name == "TEST_PRODUCT_002" ||
                x.Name == "TEST_PRODUCT_003")
            .ToList();

        Assert.Equal(3, testProducts.Count);

        Assert.Contains(
            testProducts,
            x => x.Name == "TEST_PRODUCT_001");

        Assert.Contains(
            testProducts,
            x => x.Name == "TEST_PRODUCT_002");

        Assert.Contains(
            testProducts,
            x => x.Name == "TEST_PRODUCT_003");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenIdExists()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var product = await context.Products
            .AsNoTracking()
            .FirstAsync(x => x.Name == "TEST_PRODUCT_001");

        var result = await repository.GetByIdAsync(product.Id);

        Assert.NotNull(result);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal("TEST_PRODUCT_001", result.Name);
        Assert.Equal(100.00m, result.Price);
        Assert.Equal(10, result.Stock);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var existingProduct = await context.Products
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        var nonExistingId = existingProduct.Id + 1000;

        var result = await repository.GetByIdAsync(nonExistingId);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldAddProduct()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        // Cleanup previous Create test data.
        var existingProduct = await context.Products
            .FirstOrDefaultAsync(x => x.Name == "TEST_PRODUCT_CREATE");

        if (existingProduct != null)
        {
            context.Products.Remove(existingProduct);
            await context.SaveChangesAsync();
        }

        var repository = new ProductRepository(context);

        var product = new ProductEntity
        {
            Name = "TEST_PRODUCT_CREATE",
            Description = "Created by repository test",
            Price = 75.25m,
            Stock = 15
        };

        var result = await repository.CreateAsync(product);

        Assert.True(result > 0);

        var savedProduct = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Name == "TEST_PRODUCT_CREATE");

        Assert.NotNull(savedProduct);

        Assert.Equal("TEST_PRODUCT_CREATE", savedProduct.Name);
        Assert.Equal(
            "Created by repository test",
            savedProduct.Description);
        Assert.Equal(75.25m, savedProduct.Price);
        Assert.Equal(15, savedProduct.Stock);

        // Cleanup.
        // 'product' is already tracked by the DbContext.
        context.Products.Remove(product);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var product = await context.Products
            .FirstAsync(x => x.Name == "TEST_PRODUCT_001");

        var productId = product.Id;

        product.Name = "TEST_PRODUCT_001_UPDATED";
        product.Price = 125.75m;
        product.Stock = 50;

        var result = await repository.UpdateAsync(product);

        Assert.True(result);

        // Verify actual database state.
        var updatedProduct = await context.Products
            .AsNoTracking()
            .FirstAsync(x => x.Id == productId);

        Assert.Equal(productId, updatedProduct.Id);
        Assert.Equal(
            "TEST_PRODUCT_001_UPDATED",
            updatedProduct.Name);
        Assert.Equal(125.75m, updatedProduct.Price);
        Assert.Equal(50, updatedProduct.Stock);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteProduct()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var product = await context.Products
            .FirstAsync(x => x.Name == "TEST_PRODUCT_003");

        var productId = product.Id;

        var rowVersion = product.RowVersion ?? Array.Empty<byte>();

        var result = await repository.DeleteAsync(
            productId,
            rowVersion);

        Assert.True(result);

        // Verify actual database state.
        var deletedProduct = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId);

        Assert.Null(deletedProduct);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        await using var context = CreateDbContext();

        await SeedTestDataAsync(context);

        var repository = new ProductRepository(context);

        var existingProduct = await context.Products
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        var nonExistingId = existingProduct.Id + 1000;

        var result = await repository.DeleteAsync(
            nonExistingId,
            Array.Empty<byte>());

        Assert.False(result);
    }
}
