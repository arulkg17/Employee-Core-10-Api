using Invoice.DAL.Repositories;
using Invoice.Data.Db;
using Invoice.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Invoice.DAL.Test;

/// <summary>All Receipt / Sales Invoice / PO-workflow DB tests share this collection so they never run in parallel.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class TransactionDbCollection
{
    public const string Name = "TransactionDb";
}

public sealed record SeedData(
    int VendorId,
    int CustomerId,
    int InactiveCustomerId,
    int ItemAId,
    int ItemBId,
    int InactiveItemId);

public sealed record PoSeed(
    int PurchaseOrderId,
    int LineAId,
    int LineBId,
    int ItemAId,
    int ItemBId);

public static class TxTestHelper
{
    // Everything created by these tests carries this CreatedBy so cleanup never touches other data.
    public const string TestUser = "TXT_USER";

    public static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(TestDatabase.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    // ------------------------------------------------------------
    // Cleanup (child tables first because of FKs)
    // ------------------------------------------------------------
    public static async Task CleanupAsync(AppDbContext c)
    {
        await c.Database.ExecuteSqlRawAsync(
            """
            DELETE D FROM dbo.SalesInvoiceDetail D INNER JOIN dbo.SalesInvoice H ON H.Id = D.SalesInvoiceId WHERE H.CreatedBy = 'TXT_USER';
            DELETE FROM dbo.SalesInvoice WHERE CreatedBy = 'TXT_USER';
            DELETE D FROM dbo.ReceiptDetail D INNER JOIN dbo.Receipt H ON H.Id = D.ReceiptId WHERE H.CreatedBy = 'TXT_USER';
            DELETE FROM dbo.Receipt WHERE CreatedBy = 'TXT_USER';
            DELETE D FROM dbo.PurchaseOrderDetail D INNER JOIN dbo.PurchaseOrder H ON H.Id = D.PurchaseOrderId WHERE H.CreatedBy = 'TXT_USER';
            DELETE FROM dbo.PurchaseOrder WHERE CreatedBy = 'TXT_USER';
            DELETE FROM dbo.Itemmaster WHERE CreatedBy = 'TXT_USER';
            DELETE FROM dbo.Category WHERE CreatedBy = 'TXT_USER';
            DELETE FROM dbo.Vendor WHERE CreatedBy = 'TXT_USER';
            DELETE FROM dbo.Customer WHERE CreatedBy = 'TXT_USER';
            """);
    }

    // ------------------------------------------------------------
    // Master data: vendor, customers (active + inactive), items (active + inactive)
    // ------------------------------------------------------------
    public static async Task<SeedData> SeedAsync(AppDbContext c)
    {
        var vendor = new VendorEntity
        {
            VendorCode = "TXV01",
            VendorName = "TX Test Vendor",
            ContactPerson = "Test Contact",
            MobileNo = "9999999999",
            Email = "tx@test.com",
            Address1 = "Test Address",
            City = "Test City",
            State = "Test State",
            Country = "USA",
            ZipCode = "12345",
            GstNo = "TXGST001",
            IsActive = true,
            IsDeleted = false,
            CreatedBy = TestUser,
            CreatedDate = DateTime.UtcNow
        };

        c.Vendors.Add(vendor);

        var customer = NewCustomer("TXC01", "TX Test Customer", isActive: true);
        var inactiveCustomer = NewCustomer("TXC02", "TX Inactive Customer", isActive: false);

        c.Customers.Add(customer);
        c.Customers.Add(inactiveCustomer);

        var category = new CategoryEntity
        {
            Code = "TXCAT",
            Name = "TX Test Category",
            Description = "TX tests",
            IsActive = true,
            CreatedBy = TestUser,
            CreatedDate = DateTime.UtcNow
        };

        c.Category.Add(category);

        await c.SaveChangesAsync();

        var itemA = NewItem(category.Id, "TXA01", "TX Item A", true);
        var itemB = NewItem(category.Id, "TXB01", "TX Item B", true);
        var inactiveItem = NewItem(category.Id, "TXI01", "TX Inactive Item", false);

        c.Itemmasters.AddRange(itemA, itemB, inactiveItem);

        await c.SaveChangesAsync();

        return new SeedData(
            vendor.Id, customer.Id, inactiveCustomer.Id,
            itemA.Id, itemB.Id, inactiveItem.Id);
    }

    private static CustomerEntity NewCustomer(string code, string name, bool isActive) => new()
    {
        CustomerCode = code,
        CustomerName = name,
        ContactPerson = "Test Contact",
        MobileNo = "8888888888",
        Email = "txcust@test.com",
        Address1 = "Test Address",
        City = "Test City",
        State = "Test State",
        Country = "USA",
        ZipCode = "12345",
        GstNo = "TXGST002",
        IsActive = isActive,
        IsDeleted = false,
        CreatedBy = TestUser,
        CreatedDate = DateTime.UtcNow
    };

    private static ItemmasterEntity NewItem(int categoryId, string code, string name, bool isActive) => new()
    {
        CategoryId = categoryId,
        ItemBarCode = "BAR-" + code,
        ItemCode = code,
        ItemName = name,
        Description = "TX test item",
        Uom = "KG",
        Rate = 100m,
        MinimumStock = 0m,
        MaximumStock = 1000m,
        IsActive = isActive,
        CreatedBy = TestUser,
        CreatedDate = DateTime.UtcNow
    };

    // ------------------------------------------------------------
    // Purchase order: line A = qtyA x 100 (10% tax), line B = qtyB x 200 (10% tax)
    // ------------------------------------------------------------
    public static async Task<PoSeed> CreatePoAsync(
        AppDbContext c,
        SeedData s,
        string poNumber,
        string status = "Approved",
        decimal qtyA = 10m,
        decimal qtyB = 5m)
    {
        var po = new PurchaseOrderEntity
        {
            PONumber = poNumber,
            PODate = DateTime.Now,
            VendorId = s.VendorId,
            Status = status,
            Notes = "TX test PO",
            SubTotal = 0,
            TaxAmount = 0,
            TotalAmount = 0,
            IsDeleted = false,
            CreatedBy = TestUser,
            CreatedDate = DateTime.Now
        };

        c.PurchaseOrders.Add(po);
        await c.SaveChangesAsync();

        var lineA = NewPoLine(po.Id, s.ItemAId, qtyA, 100m);
        var lineB = NewPoLine(po.Id, s.ItemBId, qtyB, 200m);

        c.PurchaseOrderDetails.AddRange(lineA, lineB);
        await c.SaveChangesAsync();

        return new PoSeed(po.Id, lineA.Id, lineB.Id, s.ItemAId, s.ItemBId);
    }

    private static PurchaseOrderDetailEntity NewPoLine(int poId, int itemId, decimal qty, decimal rate) => new()
    {
        PurchaseOrderId = poId,
        ItemmasterId = itemId,
        Quantity = qty,
        Rate = rate,
        DiscountAmount = 0,
        TaxPercent = 10,
        TaxAmount = qty * rate * 0.10m,
        LineTotal = qty * rate * 1.10m,
        ReceivedQuantity = 0
    };

    // ------------------------------------------------------------
    // Receipts (through the real repositories / stored procedures)
    // ------------------------------------------------------------
    public static async Task<int> CreateDraftReceiptAsync(
        AppDbContext c,
        PoSeed po,
        decimal qtyA,
        decimal qtyB = 0m,
        string? number = "TXT-GRN-001")
    {
        var receipts = new ReceiptRepositoryEFSp(c);
        var details = new ReceiptDetailRepositoryEFSp(c);

        var id = await receipts.AddAsync(new ReceiptEntity
        {
            ReceiptNumber = number ?? string.Empty,
            ReceiptDate = DateTime.Today,
            PurchaseOrderId = po.PurchaseOrderId,
            Notes = "TX receipt",
            CreatedBy = TestUser
        });

        if (qtyA > 0)
            await details.AddAsync(NewReceiptLine(id, po.LineAId, po.ItemAId, qtyA, 100m));

        if (qtyB > 0)
            await details.AddAsync(NewReceiptLine(id, po.LineBId, po.ItemBId, qtyB, 200m));

        return id;
    }

    public static ReceiptDetailEntity NewReceiptLine(
        int receiptId, int poLineId, int itemId, decimal qty, decimal rate) => new()
        {
            ReceiptId = receiptId,
            PurchaseOrderDetailId = poLineId,
            ItemmasterId = itemId,
            ReceivedQuantity = qty,
            Rate = rate,
            DiscountAmount = 0,
            TaxPercent = 10,
            TaxAmount = qty * rate * 0.10m,
            LineTotal = qty * rate * 1.10m
        };

    /// <summary>Creates, fills and POSTS a receipt so the items have stock.</summary>
    public static async Task<int> ReceiveStockAsync(
        AppDbContext c,
        PoSeed po,
        decimal qtyA,
        decimal qtyB = 0m,
        string number = "TXT-GRN-STOCK")
    {
        var id = await CreateDraftReceiptAsync(c, po, qtyA, qtyB, number);

        await new ReceiptRepositoryEFSp(c).PostAsync(id, TestUser);

        return id;
    }

    // ------------------------------------------------------------
    // Sales invoices
    // ------------------------------------------------------------
    public static async Task<int> CreateDraftInvoiceAsync(
        AppDbContext c,
        SeedData s,
        decimal qtyA,
        decimal qtyB = 0m,
        string? number = "TXT-INV-001")
    {
        var invoices = new SalesInvoiceRepositoryEFSp(c);
        var details = new SalesInvoiceDetailRepositoryEFSp(c);

        var id = await invoices.AddAsync(new SalesInvoiceEntity
        {
            InvoiceNumber = number ?? string.Empty,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            CustomerId = s.CustomerId,
            Notes = "TX invoice",
            CreatedBy = TestUser
        });

        if (qtyA > 0)
            await details.AddAsync(NewInvoiceLine(id, s.ItemAId, qtyA, 150m));

        if (qtyB > 0)
            await details.AddAsync(NewInvoiceLine(id, s.ItemBId, qtyB, 250m));

        return id;
    }

    public static SalesInvoiceDetailEntity NewInvoiceLine(
        int invoiceId, int itemId, decimal qty, decimal rate) => new()
        {
            SalesInvoiceId = invoiceId,
            ItemmasterId = itemId,
            Quantity = qty,
            Rate = rate,
            DiscountAmount = 0,
            TaxPercent = 10,
            TaxAmount = qty * rate * 0.10m,
            LineTotal = qty * rate * 1.10m
        };

    public static async Task<decimal> OnHandAsync(AppDbContext c, int itemId)
    {
        var stock = await new StockRepositoryEFSp(c).GetByItemmasterIdAsync(itemId);

        return stock?.OnHandQuantity ?? 0m;
    }
}
