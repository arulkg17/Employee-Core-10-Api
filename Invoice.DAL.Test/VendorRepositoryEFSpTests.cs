using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Invoice.DAL.Test;

public class VendorRepositoryEFSpTests
{
    private static string ConnectionString = TestDatabase.ConnectionString;
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
        var testVendors = await context.Vendors
            .Where(x =>
                x.VendorCode == "TSTV001" ||
                x.VendorCode == "TSTV002" ||
                x.VendorCode == "TSTV003" ||
                x.VendorCode == "TSTV004")
            .ToListAsync();

        if (testVendors.Count > 0)
        {
            context.Vendors.RemoveRange(testVendors);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // TEST DATA SEEDING
    // ============================================================

    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        await CleanupTestDataAsync(context);

        var vendors = new List<VendorEntity>
        {
            new VendorEntity
            {
                VendorCode = "TSTV001",
                VendorName = "Test Vendor One",
                ContactPerson = "Vendor Contact One",
                MobileNo = "9000000001",
                Email = "vendor1@test.com",
                Address1 = "Vendor Address One",
                Address2 = null,
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "600001",
                GstNo = "VGST001",
                IsActive = true,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now,
                UpdatedBy = null,
                UpdatedDate = null
            },

            new VendorEntity
            {
                VendorCode = "TSTV002",
                VendorName = "Test Vendor Two",
                ContactPerson = "Vendor Contact Two",
                MobileNo = "9000000002",
                Email = "vendor2@test.com",
                Address1 = "Vendor Address Two",
                Address2 = null,
                City = "Bangalore",
                State = "Karnataka",
                Country = "India",
                ZipCode = "560001",
                GstNo = "VGST002",
                IsActive = true,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now,
                UpdatedBy = null,
                UpdatedDate = null
            },

            new VendorEntity
            {
                VendorCode = "TSTV003",
                VendorName = "Test Vendor Three",
                ContactPerson = "Vendor Contact Three",
                MobileNo = "9000000003",
                Email = "vendor3@test.com",
                Address1 = "Vendor Address Three",
                Address2 = null,
                City = "Chennai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "600002",
                GstNo = "VGST003",
                IsActive = false,
                IsDeleted = false,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now,
                UpdatedBy = null,
                UpdatedDate = null
            },

            new VendorEntity
            {
                VendorCode = "TSTV004",
                VendorName = "Test Vendor Four",
                ContactPerson = "Vendor Contact Four",
                MobileNo = "9000000004",
                Email = "vendor4@test.com",
                Address1 = "Vendor Address Four",
                Address2 = null,
                City = "Madurai",
                State = "Tamil Nadu",
                Country = "India",
                ZipCode = "625001",
                GstNo = "VGST004",
                IsActive = true,
                IsDeleted = true,
                CreatedBy = "TEST",
                CreatedDate = DateTime.Now,
                UpdatedBy = null,
                UpdatedDate = null
            }
        };

        await context.Vendors.AddRangeAsync(vendors);
        await context.SaveChangesAsync();
    }

    // ============================================================
    // 1. GET ALL
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTestVendors()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllAsync();

        var testVendors = result
            .Where(x =>
                x.VendorCode == "TSTV001" ||
                x.VendorCode == "TSTV002" ||
                x.VendorCode == "TSTV003" ||
                x.VendorCode == "TSTV004")
            .ToList();

        Assert.Equal(4, testVendors.Count);
    }

    // ============================================================
    // 2. GET ALL - ACTIVE / INACTIVE / DELETED
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveInactiveAndDeletedVendors()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllAsync();

        var testVendors = result
            .Where(x =>
                x.VendorCode == "TSTV001" ||
                x.VendorCode == "TSTV002" ||
                x.VendorCode == "TSTV003" ||
                x.VendorCode == "TSTV004")
            .ToList();

        Assert.Equal(4, testVendors.Count);

        // TSTV001, TSTV002 and TSTV004 are active.
        Assert.Equal(3, testVendors.Count(x => x.IsActive == true));

        // TSTV003 is inactive.
        Assert.Equal(1, testVendors.Count(x => x.IsActive == false));

        // TSTV004 is deleted.
        Assert.Equal(1, testVendors.Count(x => x.IsDeleted == true));
    }

    // ============================================================
    // 3. GET BY ID - EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnVendor_WhenIdExists()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var vendor = await context.Vendors
            .FirstAsync(x => x.VendorCode == "TSTV001");

        var result = await repository.GetByIdAsync(vendor.Id);

        Assert.NotNull(result);
        Assert.Equal("TSTV001", result!.VendorCode);
        Assert.Equal("Test Vendor One", result.VendorName);
    }

    // ============================================================
    // 4. GET BY ID - NOT EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var maxId = await context.Vendors.MaxAsync(x => x.Id);

        var result = await repository.GetByIdAsync(maxId + 1000);

        Assert.Null(result);
    }

    // ============================================================
    // 5. ADD
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldAddVendor()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await CleanupTestDataAsync(context);

        var vendor = new VendorEntity
        {
            VendorCode = "TSTV001",
            VendorName = "Added Vendor",
            ContactPerson = "Added Vendor Contact",
            MobileNo = "9111111111",
            Email = "addedvendor@test.com",
            Address1 = "Added Vendor Address",
            Address2 = null,
            City = "Chennai",
            State = "Tamil Nadu",
            Country = "India",
            ZipCode = "600010",
            GstNo = "VGSTADD"
        };

        await repository.AddAsync(vendor);

        var inserted = await context.Vendors
            .FirstOrDefaultAsync(x => x.VendorCode == "TSTV001");

        Assert.NotNull(inserted);

        Assert.Equal("Added Vendor", inserted!.VendorName);
        Assert.Equal("Added Vendor Contact", inserted.ContactPerson);
        Assert.Equal("9111111111", inserted.MobileNo);
        Assert.Equal("addedvendor@test.com", inserted.Email);
        Assert.Equal("Chennai", inserted.City);

        // Expected defaults from the Vendor insert procedure/table.
        Assert.True(inserted.IsActive == true);
        Assert.True(inserted.IsDeleted == false);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 6. UPDATE
    // ============================================================

    [Fact]
    public async Task UpdateAsync_ShouldUpdateVendor()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var vendor = await context.Vendors
            .FirstAsync(x => x.VendorCode == "TSTV001");

        vendor.VendorName = "Updated Vendor";
        vendor.ContactPerson = "Updated Contact";
        vendor.MobileNo = "9222222222";
        vendor.Email = "updatedvendor@test.com";
        vendor.City = "Coimbatore";
        vendor.State = "Tamil Nadu";
        vendor.IsActive = false;
        vendor.IsDeleted = false;

        // Execute update.
        await repository.UpdateAsync(vendor);

        // Verify actual database state.
        var updated = await context.Vendors
            .AsNoTracking()
            .FirstAsync(x => x.Id == vendor.Id);

        Assert.Equal("Updated Vendor", updated.VendorName);
        Assert.Equal("Updated Contact", updated.ContactPerson);
        Assert.Equal("9222222222", updated.MobileNo);
        Assert.Equal("updatedvendor@test.com", updated.Email);
        Assert.Equal("Coimbatore", updated.City);
        Assert.Equal("Tamil Nadu", updated.State);

        Assert.True(updated.IsActive == false);
        Assert.True(updated.IsDeleted == false);

        Assert.NotNull(updated.UpdatedDate);
    }

    // ============================================================
    // 7. DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_ShouldDeleteVendor()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var vendor = await context.Vendors
            .FirstAsync(x => x.VendorCode == "TSTV001");

        var vendorId = vendor.Id;

        // Execute delete.
        await repository.DeleteAsync(vendorId);

        // Verify actual database state.
        var deleted = await context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vendorId);

        Assert.Null(deleted);
    }

    // ============================================================
    // 8. PAGING
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedVendors()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

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
    // 9. FILTER BY VENDOR CODE
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByVendorCode()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            "TSTV001",
            null,
            null,
            null,
            1,
            10);

        var vendors = result.Data.ToList();

        Assert.Single(vendors);

        Assert.Equal("TSTV001", vendors[0].VendorCode);
        Assert.Equal("Test Vendor One", vendors[0].VendorName);

        Assert.Equal(1, result.TotalRecords);
    }

    // ============================================================
    // 10. FILTER BY VENDOR NAME
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByVendorName()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            null,
            "Vendor Two",
            null,
            null,
            1,
            10);

        var vendors = result.Data.ToList();

        Assert.Single(vendors);

        Assert.Equal("TSTV002", vendors[0].VendorCode);
        Assert.Equal("Test Vendor Two", vendors[0].VendorName);

        Assert.Equal(1, result.TotalRecords);
    }

    // ============================================================
    // 11. FILTER BY CITY
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByCity()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await SeedTestDataAsync(context);

        var result = await repository.GetAllPagedAsync(
            null,
            null,
            null,
            "Chennai",
            1,
            10);

        var vendors = result.Data.ToList();

        Assert.Equal(2, vendors.Count);

        Assert.All(
            vendors,
            vendor => Assert.Equal("Chennai", vendor.City));

        Assert.Equal(2, result.TotalRecords);
    }

    // ============================================================
    // 12. VENDOR COUNT - ALL
    // ============================================================

    [Fact]
    public async Task GetVendorCountAsync_ShouldReturnAllVendors()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await CleanupTestDataAsync(context);

        var baselineCount =
            await repository.GetVendorCountAsync(null);

        await SeedTestDataAsync(context);

        var result =
            await repository.GetVendorCountAsync(null);

        // GetVendorCountAsync uses GetAllAsync()
        // and does NOT exclude deleted vendors.
        //
        // Therefore:
        // TSTV001 = active
        // TSTV002 = active
        // TSTV003 = inactive
        // TSTV004 = active + deleted
        //
        // All 4 are counted.
        Assert.Equal(baselineCount + 4, result);
    }

    // ============================================================
    // 13. VENDOR COUNT - ACTIVE / INACTIVE
    // ============================================================

    [Fact]
    public async Task GetVendorCountAsync_ShouldReturnCorrectActiveAndInactiveCounts()
    {
        await using var context = CreateDbContext();
        var repository = new VendorRepositoryEFSp(context);

        await CleanupTestDataAsync(context);

        var baselineActive =
            await repository.GetVendorCountAsync(true);

        var baselineInactive =
            await repository.GetVendorCountAsync(false);

        await SeedTestDataAsync(context);

        var activeCount =
            await repository.GetVendorCountAsync(true);

        var inactiveCount =
            await repository.GetVendorCountAsync(false);

        // Active vendors:
        // TSTV001 + TSTV002 + TSTV004 = 3
        //
        // NOTE:
        // TSTV004 is deleted, but GetVendorCountAsync()
        // does not filter IsDeleted.
        Assert.Equal(baselineActive + 3, activeCount);

        // Inactive vendors:
        // TSTV003 = 1
        Assert.Equal(baselineInactive + 1, inactiveCount);
    }
}
