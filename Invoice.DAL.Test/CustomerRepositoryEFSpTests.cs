using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Invoice.DAL.Test;

public class CustomerRepositoryEFSpTests
{
    private const string ConnectionString =
        "Server=LAPTOP-BIG8QIRC,1435;Database=Accounts_Test;User Id=sa;Password=123456;Encrypt=False;TrustServerCertificate=True";

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    // ============================================================
    // TEST DATA CLEANUP
    // ============================================================

    private static async Task CleanupTestDataAsync(AppDbContext context)
    {
        var testCustomers = await context.Customers
            .Where(x =>
                x.CustomerCode == "TSTC001" ||
                x.CustomerCode == "TSTC002" ||
                x.CustomerCode == "TSTC003" ||
                x.CustomerCode == "TSTC004")
            .ToListAsync();

        if (testCustomers.Count > 0)
        {
            context.Customers.RemoveRange(testCustomers);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // TEST DATA SEEDING
    // ============================================================

    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        await CleanupTestDataAsync(context);

        var customers = new List<CustomerEntity>
        {
            new CustomerEntity
            {
                CustomerCode = "TSTC001",
                CustomerName = "Test Customer One",
                ContactPerson = "Contact One",
                MobileNo = "9000000001",
                Email = "customer1@test.com",
                Address1 = "Address One",
                Address2 = null,
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "600001",
                GstNo = "GST001",
                IsActive = true,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now
            },

            new CustomerEntity
            {
                CustomerCode = "TSTC002",
                CustomerName = "Test Customer Two",
                ContactPerson = "Contact Two",
                MobileNo = "9000000002",
                Email = "customer2@test.com",
                Address1 = "Address Two",
                Address2 = null,
                City = "Bangalore",
                State = "Karnataka",
                Country = "India",
                ZipCode = "560001",
                GstNo = "GST002",
                IsActive = true,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now
            },

            new CustomerEntity
            {
                CustomerCode = "TSTC003",
                CustomerName = "Test Customer Three",
                ContactPerson = "Contact Three",
                MobileNo = "9000000003",
                Email = "customer3@test.com",
                Address1 = "Address Three",
                Address2 = null,
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "600002",
                GstNo = "GST003",
                IsActive = false,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now
            },

            new CustomerEntity
            {
                CustomerCode = "TSTC004",
                CustomerName = "Test Customer Four",
                ContactPerson = "Contact Four",
                MobileNo = "9000000004",
                Email = "customer4@test.com",
                Address1 = "Address Four",
                Address2 = null,
                City = "Madurai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "625001",
                GstNo = "GST004",
                IsActive = true,
                IsDeleted = true,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now
            }
        };

        await context.Customers.AddRangeAsync(customers);
        await context.SaveChangesAsync();
    }

    // ============================================================
    // 1. GET ALL
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestCustomers()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllAsync();

        var testCustomers = result
            .Where(x =>
                x.CustomerCode == "TSTC001" ||
                x.CustomerCode == "TSTC002" ||
                x.CustomerCode == "TSTC003" ||
                x.CustomerCode == "TSTC004")
            .ToList();

        Assert.Equal(4, testCustomers.Count);
    }

    // ============================================================
    // 2. GET ALL - ACTIVE / INACTIVE / DELETED
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveInactiveAndDeletedCustomers()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllAsync();

        var testCustomers = result
            .Where(x =>
                x.CustomerCode == "TSTC001" ||
                x.CustomerCode == "TSTC002" ||
                x.CustomerCode == "TSTC003" ||
                x.CustomerCode == "TSTC004")
            .ToList();

        Assert.Equal(4, testCustomers.Count);

        // TSTC001, TSTC002 and TSTC004 are active.
        // TSTC004 is also deleted, but GetAll does not filter deleted records.
        Assert.Equal(3, testCustomers.Count(x => x.IsActive == true));

        // TSTC003 is inactive.
        Assert.Equal(1, testCustomers.Count(x => x.IsActive == false));

        // TSTC004 is deleted.
        Assert.Equal(1, testCustomers.Count(x => x.IsDeleted == true));
    }

    // ============================================================
    // 3. GET BY ID - EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCustomer_WhenIdExists()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var customer = await context.Customers
            .FirstAsync(x => x.CustomerCode == "TSTC001");

        var result = await repository.GetByIdAsync(customer.Id);

        Assert.NotNull(result);
        Assert.Equal("TSTC001", result!.CustomerCode);
        Assert.Equal("Test Customer One", result.CustomerName);
    }

    // ============================================================
    // 4. GET BY ID - NOT EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var maxId = await context.Customers.MaxAsync(x => x.Id);

        var result = await repository.GetByIdAsync(maxId + 1000);

        Assert.Null(result);
    }

    // ============================================================
    // 5. ADD
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldAddCustomer()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await CleanupTestDataAsync(context);

        var customer = new CustomerEntity
        {
            CustomerCode = "TSTC001",
            CustomerName = "Added Customer",
            ContactPerson = "Added Contact",
            MobileNo = "9111111111",
            Email = "added@test.com",
            Address1 = "Added Address",
            City = "Chennai",
            State = "Tamil Nadu",
            Country = "India",
            ZipCode = "600010",
            GstNo = "GSTADD"
        };

        await repository.AddAsync(customer);

        var inserted = await context.Customers
            .FirstOrDefaultAsync(x => x.CustomerCode == "TSTC001");

        Assert.NotNull(inserted);
        Assert.Equal("Added Customer", inserted!.CustomerName);
        Assert.Equal("9111111111", inserted.MobileNo);

        // Insert stored procedure should initialize these values.
        Assert.True(inserted.IsActive == true);
        Assert.True(inserted.IsDeleted == false);

        // Cleanup
        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 6. UPDATE
    // ============================================================

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCustomer()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var customer = await context.Customers
            .FirstAsync(x => x.CustomerCode == "TSTC001");

        customer.CustomerName = "Updated Customer";
        customer.MobileNo = "9222222222";
        customer.City = "Coimbatore";
        customer.IsActive = false;
        customer.IsDeleted = false;

        await repository.UpdateAsync(customer);

        var updated = await context.Customers
            .AsNoTracking()
            .FirstAsync(x => x.Id == customer.Id);

        Assert.Equal("Updated Customer", updated.CustomerName);
        Assert.Equal("9222222222", updated.MobileNo);
        Assert.Equal("Coimbatore", updated.City);
        Assert.True(updated.IsActive == false);
        Assert.True(updated.IsDeleted == false);
    }

    // ============================================================
    // 7. DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCustomer()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var customer = await context.Customers
            .FirstAsync(x => x.CustomerCode == "TSTC001");

        var customerId = customer.Id;

        await repository.DeleteAsync(customerId);

        var deleted = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        Assert.Null(deleted);
    }

    // ============================================================
    // 8. PAGING
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedCustomers()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            null,
            null,
            null,
            null,
            1,
            2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count());
        Assert.True(result.TotalRecords >= 4);
    }

    // ============================================================
    // 9. FILTER BY CUSTOMER CODE
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCustomerCode()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            "TSTC001",
            null,
            null,
            null,
            1,
            10);

        var customers = result.Data.ToList();

        Assert.Single(customers);
        Assert.Equal("TSTC001", customers[0].CustomerCode);
        Assert.Equal(1, result.TotalRecords);
    }

    // ============================================================
    // 10. FILTER BY CUSTOMER NAME
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCustomerName()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            null,
            "Customer Two",
            null,
            null,
            1,
            10);

        var customers = result.Data.ToList();

        Assert.Single(customers);
        Assert.Equal("TSTC002", customers[0].CustomerCode);
        Assert.Equal("Test Customer Two", customers[0].CustomerName);
    }

    // ============================================================
    // 11. FILTER BY CITY
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCity()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            null,
            null,
            null,
            "Chennai",
            1,
            10);

        var customers = result.Data.ToList();

        Assert.Equal(2, customers.Count);
        Assert.All(customers, x => Assert.Equal("Chennai", x.City));
    }

    // ============================================================
    // 12. CUSTOMER COUNT - EXCLUDE DELETED
    // ============================================================

    [Fact]
    public async Task GetCustomerCountAsync_ShouldExcludeDeletedCustomers()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        // Make sure previous test data does not affect baseline.
        await CleanupTestDataAsync(context);

        var baselineCount = await repository.GetCustomerCountAsync(null);

        await SeedTestDataAsync(context);

        var result = await repository.GetCustomerCountAsync(null);

        // TSTC001, TSTC002 and TSTC003 should be counted.
        // TSTC004 is deleted and should not be counted.
        Assert.Equal(baselineCount + 3, result);
    }

    // ============================================================
    // 13. CUSTOMER COUNT - ACTIVE / INACTIVE
    // ============================================================

    [Fact]
    public async Task GetCustomerCountAsync_ShouldReturnCorrectActiveAndInactiveCounts()
    {
        await using var context = CreateDbContext();
        var repository = new CustomerRepositoryEFSp(context);

        // Remove old test data before measuring baseline.
        await CleanupTestDataAsync(context);

        var baselineActive =
            await repository.GetCustomerCountAsync(true);

        var baselineInactive =
            await repository.GetCustomerCountAsync(false);

        await SeedTestDataAsync(context);

        var activeCount =
            await repository.GetCustomerCountAsync(true);

        var inactiveCount =
            await repository.GetCustomerCountAsync(false);

        // Active and not deleted:
        // TSTC001 + TSTC002 = 2
        Assert.Equal(baselineActive + 2, activeCount);

        // Inactive and not deleted:
        // TSTC003 = 1
        Assert.Equal(baselineInactive + 1, inactiveCount);
    }
}
