using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

public class ItemmasterRepositoryTests
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
        // Cleanup Itemmaster test data first because Itemmaster
        // has a foreign key to Category.
        // ------------------------------------------------------------

        var testItems = await context.Itemmasters
            .Where(x =>
                x.ItemCode == "RIIT001" ||
                x.ItemCode == "RIIT002" ||
                x.ItemCode == "VEIT001" ||
                x.ItemCode == "SNIT001")
            .ToListAsync();

        if (testItems.Count > 0)
        {
            context.Itemmasters.RemoveRange(testItems);
            await context.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // Cleanup test categories.
        // ------------------------------------------------------------

        var testCategories = await context.Category
            .Where(x =>
                x.Code == "TST01" ||
                x.Code == "TST02")
            .ToListAsync();

        if (testCategories.Count > 0)
        {
            context.Category.RemoveRange(testCategories);
            await context.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // Create test categories.
        // ------------------------------------------------------------

        var riceCategory = new CategoryEntity
        {
            Code = "TST01",
            Name = "Test Rice",
            Description = "Test rice category",
            IsActive = true,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        var vegetableCategory = new CategoryEntity
        {
            Code = "TST02",
            Name = "Test Vegetables",
            Description = "Test vegetable category",
            IsActive = true,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        context.Category.AddRange(
            riceCategory,
            vegetableCategory);

        await context.SaveChangesAsync();

        // ------------------------------------------------------------
        // Create test Itemmaster records.
        //
        // We use EF here only for test setup.
        // The repository methods themselves are tested through
        // the stored procedures.
        // ------------------------------------------------------------

        context.Itemmasters.AddRange(
            new ItemmasterEntity
            {
                CategoryId = riceCategory.Id,
                ItemBarCode = "RBC001",
                ItemCode = "RIIT001",
                ItemName = "Test Rice",
                Description = "Test rice item",
                Uom = "KG",
                Rate = 100.00m,
                MinimumStock = 10.00m,
                MaximumStock = 100.00m,
                IsActive = true,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            },
            new ItemmasterEntity
            {
                CategoryId = riceCategory.Id,
                ItemBarCode = "RBC002",
                ItemCode = "RIIT002",
                ItemName = "Test Basmati Rice",
                Description = "Test basmati rice",
                Uom = "KG",
                Rate = 150.00m,
                MinimumStock = 10.00m,
                MaximumStock = 100.00m,
                IsActive = true,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            },
            new ItemmasterEntity
            {
                CategoryId = vegetableCategory.Id,
                ItemBarCode = "VBC001",
                ItemCode = "VEIT001",
                ItemName = "Test Vegetable",
                Description = "Test vegetable item",
                Uom = "KG",
                Rate = 50.00m,
                MinimumStock = 5.00m,
                MaximumStock = 50.00m,
                IsActive = false,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestItems()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);

        var testItems = result
            .Where(x =>
                x.ItemCode == "RIIT001" ||
                x.ItemCode == "RIIT002" ||
                x.ItemCode == "VEIT001")
            .ToList();

        Assert.Equal(3, testItems.Count);

        Assert.Contains(
            testItems,
            x => x.ItemCode == "RIIT001");

        Assert.Contains(
            testItems,
            x => x.ItemCode == "RIIT002");

        Assert.Contains(
            testItems,
            x => x.ItemCode == "VEIT001");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveAndInactiveItems()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var result = await repository.GetAllAsync();

        var testItems = result
            .Where(x =>
                x.ItemCode == "RIIT001" ||
                x.ItemCode == "RIIT002" ||
                x.ItemCode == "VEIT001")
            .ToList();

        Assert.Equal(
            2,
            testItems.Count(x => x.IsActive == true));

        Assert.Single(
            testItems,
            x => x.IsActive == false);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnItem_WhenIdExists()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var item = await context.Itemmasters
            .AsNoTracking()
            .FirstAsync(x => x.ItemCode == "RIIT001");

        var result = await repository.GetByIdAsync(item.Id);

        Assert.NotNull(result);

        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.CategoryId, result.CategoryId);
        Assert.Equal("RBC001", result.ItemBarCode);
        Assert.Equal("RIIT001", result.ItemCode);
        Assert.Equal("Test Rice", result.ItemName);
        Assert.Equal("Test rice item", result.Description);
        Assert.Equal("KG", result.Uom);
        Assert.Equal(100.00m, result.Rate);
        Assert.Equal(10.00m, result.MinimumStock);
        Assert.Equal(100.00m, result.MaximumStock);
        Assert.True(result.IsActive == true);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var existingItem = await context.Itemmasters
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        var nonExistingId = existingItem.Id + 1000;

        var result = await repository.GetByIdAsync(nonExistingId);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItem()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var category = await context.Category
            .FirstAsync(x => x.Code == "TST01");

        // Cleanup previous AddAsync test data.
        var existingItem = await context.Itemmasters
            .FirstOrDefaultAsync(x => x.ItemCode == "ADDIT001");

        if (existingItem != null)
        {
            context.Itemmasters.Remove(existingItem);
            await context.SaveChangesAsync();
        }

        var repository = new ItemmasterRepositoryEFSp(context);

        var item = new ItemmasterEntity
        {
            CategoryId = category.Id,
            ItemBarCode = "ABC001",
            ItemCode = "ADDIT001",
            ItemName = "Added Test Item",
            Description = "Added by repository test",
            Uom = "PCS",
            Rate = 25.50m,
            MinimumStock = 5.00m,
            MaximumStock = 50.00m,
            IsActive = true
        };

        await repository.AddAsync(item);

        var savedItem = await context.Itemmasters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ItemCode == "ADDIT001");

        Assert.NotNull(savedItem);

        Assert.Equal(category.Id, savedItem.CategoryId);
        Assert.Equal("ABC001", savedItem.ItemBarCode);
        Assert.Equal("ADDIT001", savedItem.ItemCode);
        Assert.Equal("Added Test Item", savedItem.ItemName);
        Assert.Equal(
            "Added by repository test",
            savedItem.Description);
        Assert.Equal("PCS", savedItem.Uom);
        Assert.Equal(25.50m, savedItem.Rate);
        Assert.Equal(5.00m, savedItem.MinimumStock);
        Assert.Equal(50.00m, savedItem.MaximumStock);
        Assert.True(savedItem.IsActive == true);

        // Cleanup.
        context.Itemmasters.Remove(savedItem);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateItem()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var item = await context.Itemmasters
            .FirstAsync(x => x.ItemCode == "RIIT001");

        var itemId = item.Id;

        item.ItemName = "Updated Test Rice";
        item.Description = "Updated description";
        item.Rate = 125.75m;
        item.MinimumStock = 15.00m;
        item.MaximumStock = 150.00m;

        await repository.UpdateAsync(item);

        // Verify actual database state.
        var updatedItem = await context.Itemmasters
            .AsNoTracking()
            .FirstAsync(x => x.Id == itemId);

        Assert.Equal(itemId, updatedItem.Id);
        Assert.Equal("RIIT001", updatedItem.ItemCode);
        Assert.Equal(
            "Updated Test Rice",
            updatedItem.ItemName);
        Assert.Equal(
            "Updated description",
            updatedItem.Description);
        Assert.Equal(125.75m, updatedItem.Rate);
        Assert.Equal(15.00m, updatedItem.MinimumStock);
        Assert.Equal(150.00m, updatedItem.MaximumStock);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteItem()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var item = await context.Itemmasters
            .FirstAsync(x => x.ItemCode == "VEIT001");

        var itemId = item.Id;

        await repository.DeleteAsync(itemId);

        // Verify actual database state.
        var deletedItem = await context.Itemmasters
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == itemId);

        Assert.Null(deletedItem);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedItems()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var search = new ItemmasterFilterDto
        {
            PageNumber = 1,
            PageSize = 2
        };

        var result = await repository.GetAllPagedAsync(search);

        Assert.NotNull(result);
        Assert.NotNull(result.Data);

        var testItems = result.Data
            .Where(x =>
                x.ItemCode == "RIIT001" ||
                x.ItemCode == "RIIT002" ||
                x.ItemCode == "VEIT001")
            .ToList();

        Assert.Equal(2, testItems.Count);

        Assert.True(result.TotalRecords >= 3);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCategory()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var category = await context.Category
            .AsNoTracking()
            .FirstAsync(x => x.Code == "TST01");

        var search = new ItemmasterFilterDto
        {
            CategoryId = category.Id,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetAllPagedAsync(search);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRecords);

        Assert.All(
            result.Data,
            x => Assert.Equal(category.Id, x.CategoryId));
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByItemName()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var search = new ItemmasterFilterDto
        {
            ItemName = "Basmati",
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetAllPagedAsync(search);

        Assert.NotNull(result);

        Assert.Single(result.Data);

        Assert.Equal(
            "RIIT002",
            result.Data.First().ItemCode);

        Assert.Equal(1, result.TotalRecords);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByActiveStatus()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var search = new ItemmasterFilterDto
        {
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };

        var result = await repository.GetAllPagedAsync(search);

        Assert.NotNull(result);

        var testItems = result.Data
            .Where(x =>
                x.ItemCode == "RIIT001" ||
                x.ItemCode == "RIIT002" ||
                x.ItemCode == "VEIT001")
            .ToList();

        Assert.Equal(2, testItems.Count);

        Assert.All(
            testItems,
            x => Assert.True(x.IsActive == true));

        Assert.Equal(2, result.TotalRecords);
    }

    [Fact]
    public async Task GetActiveItemCountByCategoryAsync_ShouldReturnCorrectCount()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var category = await context.Category
            .AsNoTracking()
            .FirstAsync(x => x.Code == "TST01");

        var result =
            await repository.GetActiveItemCountByCategoryAsync(
                category.Id);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetActiveItemCountByCategoryAsync_ShouldReturnZero_WhenCategoryHasNoActiveItems()
    {
        await using var context = CreateDbContext();
        await SeedTestDataAsync(context);

        var repository = new ItemmasterRepositoryEFSp(context);

        var category = await context.Category
            .AsNoTracking()
            .FirstAsync(x => x.Code == "TST02");

        var result =
            await repository.GetActiveItemCountByCategoryAsync(
                category.Id);

        Assert.Equal(0, result);
    }
}
