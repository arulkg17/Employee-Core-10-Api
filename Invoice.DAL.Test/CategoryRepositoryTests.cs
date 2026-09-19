using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

public class CategoryRepositoryTests
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

    private static async Task SeedCategoriesAsync(AppDbContext context)
    {
        var testCategories = await context.Category
            .Where(x =>
                x.Code == "RI001" ||
                x.Code == "VE001" ||
                x.Code == "SN001")
            .ToListAsync();

        if (testCategories.Count > 0)
        {
            context.Category.RemoveRange(testCategories);
            await context.SaveChangesAsync();
        }

        context.Category.AddRange(
            new CategoryEntity
            {
                Code = "RI001",
                Name = "Rice",
                Description = "All types of rice",
                IsActive = true,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            },
            new CategoryEntity
            {
                Code = "VE001",
                Name = "Vegetables",
                Description = "Fresh vegetables",
                IsActive = true,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            },
            new CategoryEntity
            {
                Code = "SN001",
                Name = "Snacks",
                Description = "All types of snacks",
                IsActive = false,
                CreatedBy = "TestUser",
                CreatedDate = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        var result = await repository.GetAllAsync();

        Assert.NotNull(result);

        var testCategories = result
            .Where(x =>
                x.Code == "RI001" ||
                x.Code == "VE001" ||
                x.Code == "SN001")
            .ToList();

        Assert.Equal(3, testCategories.Count);

        Assert.Contains(testCategories, x => x.Name == "Rice");
        Assert.Contains(testCategories, x => x.Name == "Vegetables");
        Assert.Contains(testCategories, x => x.Name == "Snacks");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveAndInactiveCategories()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        var result = await repository.GetAllAsync();

        var testCategories = result
            .Where(x =>
                x.Code == "RI001" ||
                x.Code == "VE001" ||
                x.Code == "SN001")
            .ToList();

        Assert.Equal(
            2,
            testCategories.Count(x => x.IsActive == true));

        Assert.Single(
            testCategories,
            x => x.IsActive == false);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategory_WhenIdExists()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        var category = await context.Category
            .AsNoTracking()
            .FirstAsync(x => x.Code == "RI001");

        var result = await repository.GetByIdAsync(category.Id);

        Assert.NotNull(result);

        Assert.Equal(category.Id, result.Id);
        Assert.Equal("RI001", result.Code);
        Assert.Equal("Rice", result.Name);
        Assert.True(result.IsActive == true);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        // Get an existing category ID.
        var existingCategory = await context.Category
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        // Create an ID that cannot exist.
        var nonExistingId = existingCategory.Id + 1000;

        var result = await repository.GetByIdAsync(nonExistingId);

        Assert.Null(result);
    }

[Fact]
public async Task AddAsync_ShouldAddCategory()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        // Remove previous test data if it exists.
        var existingCategory = await context.Category
            .FirstOrDefaultAsync(x => x.Code == "FR001");

        if (existingCategory != null)
        {
            context.Category.Remove(existingCategory);
            await context.SaveChangesAsync();
        }

        var repository = new CategoryRepository(context);

        var category = new CategoryEntity
        {
            Code = "FR001",
            Name = "Fruits",
            Description = "Fresh fruits",
            IsActive = true,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        // Act
        var generatedId = await repository.AddAsync(category);

        // Assert generated ID
        Assert.True(generatedId > 0);

        // Verify record was actually inserted.
        var savedCategory = await context.Category
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "FR001");

        Assert.NotNull(savedCategory);

        Assert.Equal(generatedId, savedCategory.Id);
        Assert.Equal("FR001", savedCategory.Code);
        Assert.Equal("Fruits", savedCategory.Name);
        Assert.Equal("Fresh fruits", savedCategory.Description);
        Assert.True(savedCategory.IsActive == true);

        // Cleanup test data.
        context.Category.Remove(savedCategory);
        await context.SaveChangesAsync();
    }


    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        var category = await context.Category
            .FirstAsync(x => x.Code == "RI001");

        var categoryId = category.Id;

        category.Name = "Updated Rice";
        category.Description = "Updated description";

        await repository.UpdateAsync(category);

        // Read the record again from the database.
        var updatedCategory = await context.Category
            .AsNoTracking()
            .FirstAsync(x => x.Id == categoryId);

        Assert.Equal(categoryId, updatedCategory.Id);
        Assert.Equal("RI001", updatedCategory.Code);
        Assert.Equal("Updated Rice", updatedCategory.Name);
        Assert.Equal(
            "Updated description",
            updatedCategory.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCategory()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var repository = new CategoryRepository(context);

        var category = await context.Category
            .FirstAsync(x => x.Code == "SN001");

        var categoryId = category.Id;

        // Execute delete stored procedure.
        await repository.DeleteAsync(categoryId);

        // Verify the actual database state.
        var deletedCategory = await context.Category
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == categoryId);

        Assert.Null(deletedCategory);
    }


    

    [Fact]
    public async Task CategoryCount_ShouldReturnCorrectCount()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var count = await context.Category
            .CountAsync(x =>
                x.Code == "RI001" ||
                x.Code == "VE001" ||
                x.Code == "SN001");

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ActiveCategoryCount_ShouldReturnCorrectCount()
    {
        await using var context = CreateDbContext();
        await SeedCategoriesAsync(context);

        var count = await context.Category
            .CountAsync(x =>
                (x.Code == "RI001" ||
                 x.Code == "VE001" ||
                 x.Code == "SN001") &&
                x.IsActive == true);

        Assert.Equal(2, count);
    }
}

