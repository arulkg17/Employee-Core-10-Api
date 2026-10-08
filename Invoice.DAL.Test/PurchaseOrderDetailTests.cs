using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

[Collection("PurchaseOrderDetailTests")]
public class PurchaseOrderDetailTests
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

    private static async Task CleanupTestDataAsync(
        AppDbContext context)
    {
        // --------------------------------------------------------
        // Purchase Order Detail must be deleted first
        // because it has FK to PurchaseOrder.
        // --------------------------------------------------------

        await context.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM dbo.PurchaseOrderDetail
            WHERE PurchaseOrderId IN
            (
                SELECT Id
                FROM dbo.PurchaseOrder
                WHERE PONumber LIKE 'PODTEST-%'
            );
            """);

        // --------------------------------------------------------
        // Delete test Purchase Orders
        // --------------------------------------------------------

        await context.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM dbo.PurchaseOrder
            WHERE PONumber LIKE 'PODTEST-%';
            """);

        // --------------------------------------------------------
        // Delete test Itemmaster records
        // --------------------------------------------------------

        var testItems = await context.Itemmasters
            .Where(x =>
                x.ItemCode == "PODIT001" ||
                x.ItemCode == "PODIT002")
            .ToListAsync();

        if (testItems.Count > 0)
        {
            context.Itemmasters.RemoveRange(testItems);
            await context.SaveChangesAsync();
        }

        // --------------------------------------------------------
        // Delete test Categories
        // --------------------------------------------------------

        var testCategories = await context.Category
            .Where(x =>
                x.Code == "POC01")
            .ToListAsync();

        if (testCategories.Count > 0)
        {
            context.Category.RemoveRange(testCategories);
            await context.SaveChangesAsync();
        }

        // --------------------------------------------------------
        // Delete test Vendors
        // --------------------------------------------------------

        var testVendors = await context.Vendors
            .Where(x =>
                x.VendorCode == "PODV001" ||
                x.VendorCode == "PODV002")
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

    private static async Task<(int VendorId, int ItemmasterId)>
        SeedTestDataAsync(AppDbContext context)
    {
        await CleanupTestDataAsync(context);

        // --------------------------------------------------------
        // Create Vendor
        // --------------------------------------------------------

        var vendor = new VendorEntity
        {
            VendorCode = "PODV001",
            VendorName = "PO Detail Test Vendor",
            ContactPerson = "PO Test Contact",
            MobileNo = "9000000001",
            Email = "podtest@test.com",
            Address1 = "PO Test Address",
            City = "Chennai",
            State = "Tamil Nadu",
            Country = "India",
            ZipCode = "600001",
            GstNo = "PODGST001",
            IsActive = true,
            IsDeleted = false,
            CreatedBy = "POD_TEST",
            CreatedDate = DateTime.Now
        };

        await context.Vendors.AddAsync(vendor);
        await context.SaveChangesAsync();

        // --------------------------------------------------------
        // Create Category
        // --------------------------------------------------------

        var category = new CategoryEntity
        {
            Code = "POC01",
            Name = "PO Test Category",
            Description = "Purchase Order Test Category",
            IsActive = true,
            CreatedBy = "POD_TEST",
            CreatedDate = DateTime.Now
        };

        await context.Category.AddAsync(category);
        await context.SaveChangesAsync();

        // --------------------------------------------------------
        // Create Itemmaster
        // --------------------------------------------------------

        var item = new ItemmasterEntity
        {
            CategoryId = category.Id,
            ItemBarCode = "PODBAR001",
            ItemCode = "PODIT001",
            ItemName = "PO Test Item",
            Description = "Purchase Order Test Item",
            Uom = "PCS",
            Rate = 100.00m,
            MinimumStock = 10.00m,
            MaximumStock = 100.00m,
            IsActive = true,
            CreatedBy = "POD_TEST",
            CreatedDate = DateTime.Now
        };

        await context.Itemmasters.AddAsync(item);
        await context.SaveChangesAsync();

        return (vendor.Id, item.Id);
    }

    // ============================================================
    // CREATE PURCHASE ORDER FOR TESTING
    // ============================================================

    private static async Task<int> CreatePurchaseOrderAsync(
        AppDbContext context,
        int vendorId)
    {
        var purchaseOrder = new PurchaseOrderEntity
        {
            PONumber = "PODTEST-001",
            PODate = DateTime.Now,
            VendorId = vendorId,
            Status = "Draft",
            Notes = "Purchase Order Detail Test",
            SubTotal = 100.00m,
            TaxAmount = 10.00m,
            TotalAmount = 110.00m,
            IsDeleted = false,
            CreatedBy = "POD_TEST",
            CreatedDate = DateTime.Now
        };

        await context.PurchaseOrders.AddAsync(purchaseOrder);
        await context.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    // ============================================================
    // CREATE PURCHASE ORDER DETAIL FOR TESTING
    // ============================================================

    private static async Task<int> CreatePurchaseOrderDetailAsync(
        AppDbContext context,
        int purchaseOrderId,
        int itemmasterId,
        decimal quantity = 2.00m,
        decimal rate = 50.00m,
        decimal discountAmount = 0.00m,
        decimal taxPercent = 10.00m,
        decimal taxAmount = 10.00m,
        decimal lineTotal = 110.00m)
    {
        var detail = new PurchaseOrderDetailEntity
        {
            PurchaseOrderId = purchaseOrderId,
            ItemmasterId = itemmasterId,
            Quantity = quantity,
            Rate = rate,
            DiscountAmount = discountAmount,
            TaxPercent = taxPercent,
            TaxAmount = taxAmount,
            LineTotal = lineTotal
        };

        await context.PurchaseOrderDetails.AddAsync(detail);
        await context.SaveChangesAsync();

        return detail.Id;
    }

    // ============================================================
    // 1. ADD
    // ============================================================

    [Fact]
    public async Task AddAsync_ShouldAddPurchaseOrderDetail()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        var detail = new PurchaseOrderDetailEntity
        {
            PurchaseOrderId = purchaseOrderId,
            ItemmasterId = itemmasterId,
            Quantity = 5.00m,
            Rate = 100.00m,
            DiscountAmount = 20.00m,
            TaxPercent = 10.00m,
            TaxAmount = 48.00m,
            LineTotal = 528.00m
        };

        // Act
        var result = await repository.AddAsync(detail);

        // Assert
        Assert.True(result > 0);

        var savedDetail =
            await context.PurchaseOrderDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PurchaseOrderId == purchaseOrderId &&
                    x.ItemmasterId == itemmasterId &&
                    x.Quantity == 5.00m);

        Assert.NotNull(savedDetail);

        Assert.Equal(
            purchaseOrderId,
            savedDetail!.PurchaseOrderId);

        Assert.Equal(
            itemmasterId,
            savedDetail.ItemmasterId);

        Assert.Equal(
            5.00m,
            savedDetail.Quantity);

        Assert.Equal(
            100.00m,
            savedDetail.Rate);

        Assert.Equal(
            20.00m,
            savedDetail.DiscountAmount);

        Assert.Equal(
            10.00m,
            savedDetail.TaxPercent);

        Assert.Equal(
            48.00m,
            savedDetail.TaxAmount);

        Assert.Equal(
            528.00m,
            savedDetail.LineTotal);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 2. GET BY PURCHASE ORDER ID
    // ============================================================

    [Fact]
    public async Task GetByPurchaseOrderIdAsync_ShouldReturnDetails()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);

        await CreatePurchaseOrderDetailAsync(
            context,
            purchaseOrderId,
            itemmasterId,
            quantity: 2.00m,
            rate: 50.00m,
            discountAmount: 0.00m,
            taxPercent: 10.00m,
            taxAmount: 10.00m,
            lineTotal: 110.00m);

        await CreatePurchaseOrderDetailAsync(
            context,
            purchaseOrderId,
            itemmasterId,
            quantity: 3.00m,
            rate: 100.00m,
            discountAmount: 20.00m,
            taxPercent: 10.00m,
            taxAmount: 28.00m,
            lineTotal: 308.00m);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        // Act
        var result =
            await repository.GetByPurchaseOrderIdAsync(
                purchaseOrderId);

        // Assert
        Assert.NotNull(result);

        var details = result.ToList();

        Assert.Equal(2, details.Count);

        Assert.All(
            details,
            x => Assert.Equal(
                purchaseOrderId,
                x.PurchaseOrderId));

        Assert.Contains(
            details,
            x =>
                x.Quantity == 2.00m &&
                x.Rate == 50.00m);

        Assert.Contains(
            details,
            x =>
                x.Quantity == 3.00m &&
                x.Rate == 100.00m);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 3. GET BY ID - EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDetail_WhenIdExists()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);

        var detailId =
            await CreatePurchaseOrderDetailAsync(
                context,
                purchaseOrderId,
                itemmasterId);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        // Act
        var result =
            await repository.GetByIdAsync(detailId);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            detailId,
            result!.Id);

        Assert.Equal(
            purchaseOrderId,
            result.PurchaseOrderId);

        Assert.Equal(
            itemmasterId,
            result.ItemmasterId);

        Assert.Equal(
            2.00m,
            result.Quantity);

        Assert.Equal(
            50.00m,
            result.Rate);

        Assert.Equal(
            10.00m,
            result.TaxPercent);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 4. GET BY ID - NOT EXISTS
    // ============================================================

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        await using var context = CreateDbContext();

        await CleanupTestDataAsync(context);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        // Act
        var result =
            await repository.GetByIdAsync(999999);

        // Assert
        Assert.Null(result);
    }

    // ============================================================
    // 5. UPDATE
    // ============================================================

    [Fact]
    public async Task UpdateAsync_ShouldUpdatePurchaseOrderDetail()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);
        var purchaseOrderExists =
            await context.PurchaseOrders
            .AnyAsync(x => x.Id == purchaseOrderId);

        Assert.True(
            purchaseOrderExists,
            $"PurchaseOrder with Id {purchaseOrderId} does not exist.");

        var detailId =
            await CreatePurchaseOrderDetailAsync(
                context,
                purchaseOrderId,
                itemmasterId);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        var updatedDetail =
            new PurchaseOrderDetailEntity
            {
                Id = detailId,
                PurchaseOrderId = purchaseOrderId,
                ItemmasterId = itemmasterId,
                Quantity = 10.00m,
                Rate = 200.00m,
                DiscountAmount = 50.00m,
                TaxPercent = 15.00m,
                TaxAmount = 292.50m,
                LineTotal = 2242.50m
            };

        // Act
        var result =
            await repository.UpdateAsync(
                updatedDetail);

        // Assert
        Assert.True(result);

        var savedDetail =
            await context.PurchaseOrderDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == detailId);

        Assert.NotNull(savedDetail);

        Assert.Equal(
            purchaseOrderId,
            savedDetail!.PurchaseOrderId);

        Assert.Equal(
            itemmasterId,
            savedDetail.ItemmasterId);

        Assert.Equal(
            10.00m,
            savedDetail.Quantity);

        Assert.Equal(
            200.00m,
            savedDetail.Rate);

        Assert.Equal(
            50.00m,
            savedDetail.DiscountAmount);

        Assert.Equal(
            15.00m,
            savedDetail.TaxPercent);

        Assert.Equal(
            292.50m,
            savedDetail.TaxAmount);

        Assert.Equal(
            2242.50m,
            savedDetail.LineTotal);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 6. DELETE
    // ============================================================

    [Fact]
    public async Task DeleteAsync_ShouldDeletePurchaseOrderDetail()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);

        var detailId =
            await CreatePurchaseOrderDetailAsync(
                context,
                purchaseOrderId,
                itemmasterId);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        // Act
        var result =
            await repository.DeleteAsync(detailId);

        // Assert
        Assert.True(result);

        var deletedDetail =
            await context.PurchaseOrderDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == detailId);

        Assert.Null(deletedDetail);

        await CleanupTestDataAsync(context);
    }

    // ============================================================
    // 7. DELETE BY PURCHASE ORDER ID
    // ============================================================

    [Fact]
    public async Task DeleteByPurchaseOrderIdAsync_ShouldDeleteAllDetails()
    {
        await using var context = CreateDbContext();

        var (vendorId, itemmasterId) =
            await SeedTestDataAsync(context);

        var purchaseOrderId =
            await CreatePurchaseOrderAsync(
                context,
                vendorId);
        var purchaseOrderExists =
            await context.PurchaseOrders
                .AnyAsync(x => x.Id == purchaseOrderId);

        Assert.True(
            purchaseOrderExists,
            $"PurchaseOrder with Id {purchaseOrderId} does not exist.");

        await CreatePurchaseOrderDetailAsync(
            context,
            purchaseOrderId,
            itemmasterId,
            quantity: 2.00m,
            rate: 50.00m);

        await CreatePurchaseOrderDetailAsync(
            context,
            purchaseOrderId,
            itemmasterId,
            quantity: 4.00m,
            rate: 75.00m);

        await CreatePurchaseOrderDetailAsync(
            context,
            purchaseOrderId,
            itemmasterId,
            quantity: 6.00m,
            rate: 100.00m);

        var repository =
            new PurchaseOrderDetailRepositoryEFSp(context);

        // --------------------------------------------------------
        // Verify setup
        // --------------------------------------------------------

        var beforeDelete =
            await context.PurchaseOrderDetails
                .AsNoTracking()
                .CountAsync(x =>
                    x.PurchaseOrderId ==
                    purchaseOrderId);

        Assert.Equal(3, beforeDelete);

        // Act
        var result =
            await repository
                .DeleteByPurchaseOrderIdAsync(
                    purchaseOrderId);

        // Assert
        Assert.True(result);

        var afterDelete =
            await context.PurchaseOrderDetails
                .AsNoTracking()
                .CountAsync(x =>
                    x.PurchaseOrderId ==
                    purchaseOrderId);

        Assert.Equal(0, afterDelete);

        await CleanupTestDataAsync(context);
    }
}