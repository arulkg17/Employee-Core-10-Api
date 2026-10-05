using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Invoice.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

public class PurchaseOrderTests
{
    private static string ConnectionString => TestDatabase.ConnectionString;

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    // ============================================================
    // Test Data Setup
    // ============================================================

    private static async Task<(int VendorId, int ItemmasterId)> SeedMasterDataAsync(
        AppDbContext context)
    {
        // --------------------------------------------------------
        // Clean existing PO test data
        // --------------------------------------------------------

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.PurchaseOrderDetail");

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.PurchaseOrder");

        // --------------------------------------------------------
        // Remove previous test Itemmaster / Vendor records
        // --------------------------------------------------------

        var testItems = await context.Itemmasters
            .Where(x => x.ItemName.StartsWith("PO_TEST_ITEM"))
            .ToListAsync();

        if (testItems.Count > 0)
        {
            context.Itemmasters.RemoveRange(testItems);
            await context.SaveChangesAsync();
        }

        var testVendors = await context.Vendors
            .Where(x => x.VendorName.StartsWith("PO_TEST_VENDOR"))
            .ToListAsync();

        if (testVendors.Count > 0)
        {
            context.Vendors.RemoveRange(testVendors);
            await context.SaveChangesAsync();
        }

        // --------------------------------------------------------
        // Create Vendor
        // --------------------------------------------------------

        var vendor = new VendorEntity
        {
            VendorCode = "POV01",
            VendorName = "PO_TEST_VENDOR_01",
            ContactPerson = "Test Contact",
            MobileNo = "9999999999",
            Email = "potest@test.com",
            Address1 = "Test Address",
            City = "Test City",
            State = "Test State",
            Country = "USA",
            ZipCode = "12345",
            GstNo = "TESTGST001",
            IsActive = true,
            IsDeleted = false,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        context.Vendors.Add(vendor);
        await context.SaveChangesAsync();

        // --------------------------------------------------------
        // Create Category
        // --------------------------------------------------------

        var category = new CategoryEntity
        {
            Code = "POC1",
            Name = "PO Test Category",
            Description = "Purchase Order Test Category",
            IsActive = true,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        
        context.Category.Add(category);
        await context.SaveChangesAsync();

        // --------------------------------------------------------
        // Create Itemmaster
        // --------------------------------------------------------

        var item = new ItemmasterEntity
        {
            CategoryId = category.Id,
            ItemBarCode = "POBAR001",
            ItemCode = "POIT001",
            ItemName = "PO Test Item",
            Description = "Purchase Order test item",
            Uom = "KG",
            Rate = 100.00m,
            MinimumStock = 10.00m,
            MaximumStock = 100.00m,
            IsActive = true,
            CreatedBy = "TestUser",
            CreatedDate = DateTime.UtcNow
        };

        context.Itemmasters.Add(item);
        await context.SaveChangesAsync();

        return (vendor.Id, item.Id);
    }

    private static async Task<int> CreatePurchaseOrderAsync(
     AppDbContext context,
     int vendorId,
     string poNumber = "PO-TEST-001",
     string status = "Draft")
    {
        var purchaseOrder = new PurchaseOrderEntity
        {
            PONumber = poNumber,
            PODate = DateTime.Now,
            VendorId = vendorId,
            Status = status,
            Notes = "Purchase Order Test",
            SubTotal = 100,
            TaxAmount = 10,
            TotalAmount = 110,
            IsDeleted = false,
            CreatedBy = "PO_TEST",
            CreatedDate = DateTime.Now
        };

        context.PurchaseOrders.Add(purchaseOrder);
        await context.SaveChangesAsync();

        return purchaseOrder.Id;
    }
    private static async Task CleanupAsync(AppDbContext context)
    {
        // Details first because of FK
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.PurchaseOrderDetail");

        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dbo.PurchaseOrder");

        // Remove test items
        var testItems = await context.Itemmasters
            .Where(x => x.ItemName.StartsWith("PO_TEST_ITEM"))
            .ToListAsync();

        if (testItems.Count > 0)
        {
            context.Itemmasters.RemoveRange(testItems);
            await context.SaveChangesAsync();
        }

        // Remove test categories
        var testCategories = await context.Category
            .Where(x => x.Name == "PO_TEST_CATEGORY")
            .ToListAsync();

        if (testCategories.Count > 0)
        {
            context.Category.RemoveRange(testCategories);
            await context.SaveChangesAsync();
        }

        // Remove test vendors
        var testVendors = await context.Vendors
            .Where(x => x.VendorName.StartsWith("PO_TEST_VENDOR"))
            .ToListAsync();

        if (testVendors.Count > 0)
        {
            context.Vendors.RemoveRange(testVendors);
            await context.SaveChangesAsync();
        }
    }

    // ============================================================
    // 1. GetAllAsync
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnTestPurchaseOrders()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-001");

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-002");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        var purchaseOrders = result
            .Where(x => x.PONumber.StartsWith("PO-TEST-"))
            .ToList();

        Assert.Equal(2, purchaseOrders.Count);
        Assert.Contains(
            purchaseOrders,
            x => x.PONumber == "PO-TEST-001");

        Assert.Contains(
            purchaseOrders,
            x => x.PONumber == "PO-TEST-002");

        await CleanupAsync(context);
    }

    // ============================================================
    // 2. GetAllAsync - Active Only
    // ============================================================

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActivePurchaseOrders()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        var activePO = new PurchaseOrderEntity
        {
            PONumber = "PO-TEST-ACTIVE",
            PODate = DateTime.Now,
            VendorId = vendorId,
            Status = "Draft",
            Notes = "Active PO",
            SubTotal = 100,
            TaxAmount = 10,
            TotalAmount = 110,
            IsDeleted = false,
            CreatedBy = "PO_TEST",
            CreatedDate = DateTime.Now
        };

        var deletedPO = new PurchaseOrderEntity
        {
            PONumber = "PO-TEST-DELETED",
            PODate = DateTime.Now,
            VendorId = vendorId,
            Status = "Draft",
            Notes = "Deleted PO",
            SubTotal = 200,
            TaxAmount = 20,
            TotalAmount = 220,
            IsDeleted = true,
            CreatedBy = "PO_TEST",
            CreatedDate = DateTime.Now
        };

        context.PurchaseOrders.AddRange(activePO, deletedPO);

        await context.SaveChangesAsync();

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Contains(
            result,
            x => x.PONumber == "PO-TEST-ACTIVE");

        Assert.DoesNotContain(
            result,
            x => x.PONumber == "PO-TEST-DELETED");

        await CleanupAsync(context);
    }

    // ============================================================
    // 3. GetByIdAsync - Found
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPurchaseOrder_WhenIdExists()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        var id = await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-BYID");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("PO-TEST-BYID", result.PONumber);
        Assert.Equal(vendorId, result.VendorId);
        Assert.Equal("Draft", result.Status);

        await CleanupAsync(context);
    }

    // ============================================================
    // 4. GetByIdAsync - Not Found
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();

        await CleanupAsync(context);

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetByIdAsync(999999);

        // Assert
        Assert.Null(result);
    }

    // ============================================================
    // 5. AddAsync
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldAddPurchaseOrder()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        var repository = new PurchaseOrderRepositoryEFSp(context);

        var purchaseOrder = new PurchaseOrderEntity
        {
            PONumber = "PO-TEST-ADD",
            PODate = DateTime.Now,
            VendorId = vendorId,
            Status = "Draft",
            Notes = "Add Test",
            SubTotal = 500,
            TaxAmount = 50,
            TotalAmount = 550,
            IsDeleted = false,
            CreatedBy = "PO_TEST"
        };

        // Act
        var id = await repository.AddAsync(purchaseOrder);

        // Assert
        Assert.True(id > 0);

        var inserted = await context.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        Assert.NotNull(inserted);
        Assert.Equal("PO-TEST-ADD", inserted!.PONumber);
        Assert.Equal(vendorId, inserted.VendorId);
        Assert.Equal(500, inserted.SubTotal);
        Assert.Equal(50, inserted.TaxAmount);
        Assert.Equal(550, inserted.TotalAmount);
        Assert.False(inserted.IsDeleted);

        await CleanupAsync(context);
    }

    // ============================================================
    // 6. UpdateAsync
    // ============================================================

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePurchaseOrder()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        var id = await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-UPDATE");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        var purchaseOrder = new PurchaseOrderEntity
        {
            Id = id,
            PONumber = "PO-TEST-UPDATED",
            PODate = DateTime.Now.AddDays(1),
            VendorId = vendorId,
            Status = "Approved",
            Notes = "Updated PO",
            SubTotal = 900,
            TaxAmount = 90,
            TotalAmount = 990,
            IsDeleted = false,
            UpdatedBy = "PO_TEST_UPDATE"
        };

        // Act
        var result = await repository.UpdateAsync(purchaseOrder);
        Console.WriteLine($"Update Result: {result}");
        // Assert
        Assert.True(result);

        var updated = await context.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        Assert.NotNull(updated);
        Assert.Equal("PO-TEST-UPDATED", updated!.PONumber);
        Assert.Equal("Approved", updated.Status);
        Assert.Equal("Updated PO", updated.Notes);
        Assert.Equal(900, updated.SubTotal);
        Assert.Equal(90, updated.TaxAmount);
        Assert.Equal(990, updated.TotalAmount);
        Assert.Equal("PO_TEST_UPDATE", updated.UpdatedBy);
        Assert.NotNull(updated.UpdatedDate);

        await CleanupAsync(context);
    }

    // ============================================================
    // 7. DeleteAsync - Soft Delete
    // ============================================================

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeletePurchaseOrder()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        var id = await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-DELETE");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.DeleteAsync(id);

        // Assert
        Assert.True(result);

        var deleted = await context.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        Assert.NotNull(deleted);
        Assert.True(deleted!.IsDeleted);

        // SP GetById should not return soft-deleted record
        var getByIdResult = await repository.GetByIdAsync(id);

        Assert.Null(getByIdResult);

        await CleanupAsync(context);
    }

    // ============================================================
    // 8. GetAllPagedAsync
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldReturnPagedPurchaseOrders()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        for (int i = 1; i <= 5; i++)
        {
            await CreatePurchaseOrderAsync(
                context,
                vendorId,
                $"PO-TEST-PAGE-{i:000}");
        }

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllPagedAsync(
            null,
            null,
            null,
            1,
            2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        await CleanupAsync(context);
    }

    // ============================================================
    // 9. GetAllPagedAsync - PO Number Filter
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByPONumber()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-APPLE");

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-BANANA");

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-APPLE-002");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllPagedAsync(
            "APPLE",
            null,
            null,
            1,
            10);

        // Assert
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        Assert.All(
            result.Data,
            x => Assert.Contains(
                "APPLE",
                x.PONumber,
                StringComparison.OrdinalIgnoreCase));

        await CleanupAsync(context);
    }

    // ============================================================
    // 10. GetAllPagedAsync - Vendor Filter
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByVendor()
    {
        await using var context = CreateDbContext();

        var (vendorId1, _) = await SeedMasterDataAsync(context);

        // Create second vendor
        var vendor2 = new VendorEntity
        {
            VendorCode = "POV02",
            VendorName = "PO_TEST_VENDOR_02",
            IsActive = true,
            IsDeleted = false,
            CreatedBy = "PO_TEST",
            CreatedDate = DateTime.Now
        };

        context.Vendors.Add(vendor2);
        await context.SaveChangesAsync();

        await CreatePurchaseOrderAsync(
            context,
            vendorId1,
            "PO-TEST-VENDOR-001");

        await CreatePurchaseOrderAsync(
            context,
            vendor2.Id,
            "PO-TEST-VENDOR-002");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllPagedAsync(
            null,
            vendor2.Id,
            null,
            1,
            10);

        // Assert
        Assert.Equal(1, result.TotalRecords);
        Assert.Single(result.Data);

        var purchaseOrder = result.Data.First();

        Assert.Equal(vendor2.Id, purchaseOrder.VendorId);
        Assert.Equal("PO-TEST-VENDOR-002", purchaseOrder.PONumber);

        await CleanupAsync(context);
    }

    // ============================================================
    // 11. GetAllPagedAsync - Status Filter
    // ============================================================

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterByStatus()
    {
        await using var context = CreateDbContext();

        var (vendorId, _) = await SeedMasterDataAsync(context);

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-DRAFT",
            "Draft");

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-APPROVED",
            "Approved");

        await CreatePurchaseOrderAsync(
            context,
            vendorId,
            "PO-TEST-APPROVED-002",
            "Approved");

        var repository = new PurchaseOrderRepositoryEFSp(context);

        // Act
        var result = await repository.GetAllPagedAsync(
            null,
            null,
            "Approved",
            1,
            10);

        // Assert
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Data.Count());

        Assert.All(
            result.Data,
            x => Assert.Equal("Approved", x.Status));

        await CleanupAsync(context);
    }
}