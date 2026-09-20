using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

public class ProductRepositorySpTests
{
    private static string ConnectionString = TestDatabase.ConnectionString;

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static ProductRepositorySp CreateRepository(AppDbContext context)
    {
        return new ProductRepositorySp(context);
    }

    private static async Task DeleteTestProductsAsync()
    {
        await using var context = CreateDbContext();

        var products = await context.Products
            .Where(x => x.Name != null &&
                        x.Name.StartsWith("SP_TEST_PRODUCT_"))
            .ToListAsync();

        if (products.Count > 0)
        {
            context.Products.RemoveRange(products);
            await context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateProduct()
    {
        await DeleteTestProductsAsync();

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var product = new ProductEntity
        {
            Name = "SP_TEST_PRODUCT_CREATE",
            Description = "Created using Product SP repository",
            Price = 125.50m,
            Stock = 25
        };

        var id = await repository.CreateAsync(product);

        Assert.True(id > 0);

        var savedProduct = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        Assert.NotNull(savedProduct);
        Assert.Equal("SP_TEST_PRODUCT_CREATE", savedProduct.Name);
        Assert.Equal(
            "Created using Product SP repository",
            savedProduct.Description);
        Assert.Equal(125.50m, savedProduct.Price);
        Assert.Equal(25, savedProduct.Stock);

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTestProducts()
    {
        await DeleteTestProductsAsync();

        await using (var seedContext = CreateDbContext())
        {
            seedContext.Products.AddRange(
                new ProductEntity
                {
                    Name = "SP_TEST_PRODUCT_ALL_001",
                    Description = "SP Test Product 001",
                    Price = 100m,
                    Stock = 10
                },
                new ProductEntity
                {
                    Name = "SP_TEST_PRODUCT_ALL_002",
                    Description = "SP Test Product 002",
                    Price = 200m,
                    Stock = 20
                });

            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var products = await repository.GetAllAsync();

        Assert.NotNull(products);

        var result = products.ToList();

        Assert.Contains(
            result,
            x => x.Name == "SP_TEST_PRODUCT_ALL_001");

        Assert.Contains(
            result,
            x => x.Name == "SP_TEST_PRODUCT_ALL_002");

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenIdExists()
    {
        await DeleteTestProductsAsync();

        int productId;

        await using (var seedContext = CreateDbContext())
        {
            var product = new ProductEntity
            {
                Name = "SP_TEST_PRODUCT_GETBYID",
                Description = "GetById SP test",
                Price = 350m,
                Stock = 30
            };

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
        }

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var result = await repository.GetByIdAsync(productId);

        Assert.NotNull(result);
        Assert.Equal(productId, result.Id);
        Assert.Equal("SP_TEST_PRODUCT_GETBYID", result.Name);
        Assert.Equal("GetById SP test", result.Description);
        Assert.Equal(350m, result.Price);
        Assert.Equal(30, result.Stock);

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await DeleteTestProductsAsync();

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var result = await repository.GetByIdAsync(-999999);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProduct()
    {
        await DeleteTestProductsAsync();

        int productId;

        await using (var seedContext = CreateDbContext())
        {
            var product = new ProductEntity
            {
                Name = "SP_TEST_PRODUCT_UPDATE",
                Description = "Before update",
                Price = 100m,
                Stock = 10
            };

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
        }

        byte[] rowVersion;

        await using (var readContext = CreateDbContext())
        {
            var product = await readContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            rowVersion = product.RowVersion.ToArray();
        }

        await using (var updateContext = CreateDbContext())
        {
            var repository = CreateRepository(updateContext);

            var product = new ProductEntity
            {
                Id = productId,
                Name = "SP_TEST_PRODUCT_UPDATE_CHANGED",
                Description = "After update",
                Price = 250m,
                Stock = 50,
                RowVersion = rowVersion
            };

            var result = await repository.UpdateAsync(product);

            Assert.True(result);
        }

        await using (var verifyContext = CreateDbContext())
        {
            var updatedProduct = await verifyContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            Assert.Equal(
                "SP_TEST_PRODUCT_UPDATE_CHANGED",
                updatedProduct.Name);

            Assert.Equal(
                "After update",
                updatedProduct.Description);

            Assert.Equal(250m, updatedProduct.Price);
            Assert.Equal(50, updatedProduct.Stock);
        }

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnFalse_WhenRowVersionIsIncorrect()
    {
        await DeleteTestProductsAsync();

        int productId;
        byte[] incorrectRowVersion;

        await using (var seedContext = CreateDbContext())
        {
            var product = new ProductEntity
            {
                Name = "SP_TEST_PRODUCT_UPDATE_CONFLICT",
                Description = "Concurrency test",
                Price = 100m,
                Stock = 10
            };

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
            incorrectRowVersion = product.RowVersion.ToArray();
        }

        // Change the RowVersion so it no longer matches the database.
        incorrectRowVersion[0] ^= 0xFF;

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var productToUpdate = new ProductEntity
        {
            Id = productId,
            Name = "SP_TEST_PRODUCT_CONFLICT_CHANGED",
            Description = "Should not update",
            Price = 999m,
            Stock = 99,
            RowVersion = incorrectRowVersion
        };

        var result = await repository.UpdateAsync(productToUpdate);

        Assert.False(result);

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteProduct()
    {
        await DeleteTestProductsAsync();

        int productId;
        byte[] rowVersion;

        await using (var seedContext = CreateDbContext())
        {
            var product = new ProductEntity
            {
                Name = "SP_TEST_PRODUCT_DELETE",
                Description = "Delete SP test",
                Price = 450m,
                Stock = 40
            };

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
            rowVersion = product.RowVersion.ToArray();
        }

        await using (var context = CreateDbContext())
        {
            var repository = CreateRepository(context);

            var result = await repository.DeleteAsync(
                productId,
                rowVersion);

            Assert.True(result);
        }

        await using (var verifyContext = CreateDbContext())
        {
            var deletedProduct = await verifyContext.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == productId);

            Assert.Null(deletedProduct);
        }

        await DeleteTestProductsAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenRowVersionIsIncorrect()
    {
        await DeleteTestProductsAsync();

        int productId;
        byte[] incorrectRowVersion;

        await using (var seedContext = CreateDbContext())
        {
            var product = new ProductEntity
            {
                Name = "SP_TEST_PRODUCT_DELETE_CONFLICT",
                Description = "Delete concurrency test",
                Price = 550m,
                Stock = 55
            };

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
            incorrectRowVersion = product.RowVersion.ToArray();
        }

        // Make the RowVersion incorrect.
        incorrectRowVersion[0] ^= 0xFF;

        await using var context = CreateDbContext();
        var repository = CreateRepository(context);

        var result = await repository.DeleteAsync(
            productId,
            incorrectRowVersion);

        Assert.False(result);

        // Confirm that the product still exists.
        var productStillExists = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == productId);

        Assert.NotNull(productStillExists);

        await DeleteTestProductsAsync();
    }
}
