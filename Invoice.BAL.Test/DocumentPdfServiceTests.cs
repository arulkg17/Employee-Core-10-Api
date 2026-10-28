using System.Text;
using Invoice.DTOs;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using Xunit;

namespace Invoice.BAL.Test.Services;

public class DocumentPdfServiceTests
{
    private readonly DocumentPdfService _sut = new();

    private static readonly Dictionary<int, string> Items = new() { [5] = "Laptop", [6] = "Mouse" };

    public DocumentPdfServiceTests() => QuestPDF.Settings.License = LicenseType.Community;

    private static bool IsPdf(byte[] b) =>
        b.Length > 500 && Encoding.ASCII.GetString(b, 0, 4) == "%PDF";

    private static string TextOf(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        return string.Join(" ", doc.GetPages().Select(p => p.Text));
    }

    private static PurchaseOrderDto Po(string status = "Draft", int lines = 2)
    {
        var po = new PurchaseOrderDto
        {
            Id = 6,
            PONumber = "PO-000006",
            PODate = new DateTime(2026, 10, 28),
            VendorId = 1,
            Status = status,
            Notes = "Deliver to main warehouse",
            SubTotal = 200,
            TaxAmount = 36,
            TotalAmount = 236
        };
        for (int i = 0; i < lines; i++)
            po.Details.Add(new PurchaseOrderDetailDto
            {
                ItemmasterId = i % 2 == 0 ? 5 : 6,
                Quantity = 2,
                Rate = 50,
                DiscountAmount = 0,
                TaxPercent = 18,
                TaxAmount = 18,
                LineTotal = 118
            });
        return po;
    }

    private static SalesInvoiceDto Invoice(string status = "Posted") => new()
    {
        Id = 3,
        InvoiceNumber = "INV-000003",
        InvoiceDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(15),
        CustomerId = 2,
        Status = status,
        SubTotal = 100,
        TaxAmount = 18,
        TotalAmount = 118,
        Details = { new SalesInvoiceDetailDto { ItemmasterId = 5, Quantity = 1, Rate = 100,
                    TaxPercent = 18, TaxAmount = 18, LineTotal = 118 } }
    };

    private static ReceiptDto Receipt(string status = "Posted", string? number = "GRN-000004") => new()
    {
        Id = 4,
        ReceiptNumber = number,
        ReceiptDate = DateTime.Today,
        PurchaseOrderId = 6,
        VendorId = 1,
        Status = status,
        SubTotal = 100,
        TaxAmount = 18,
        TotalAmount = 118,
        Details = { new ReceiptDetailDto { ItemmasterId = 5, ReceivedQuantity = 1, Rate = 100,
                    TaxPercent = 18, TaxAmount = 18, LineTotal = 118 } }
    };

    // ---------- Purchase Order ----------
    [Theory]
    [InlineData("Draft")]
    [InlineData("Approved")]
    [InlineData("Cancelled")]
    public void PurchaseOrder_AllStatuses_ReturnValidPdf(string status)
        => Assert.True(IsPdf(_sut.GeneratePurchaseOrder(Po(status), "Test Vendor", Items)));

    [Fact]
    public void PurchaseOrder_ContainsNumberVendorAndItems()
    {
        var text = TextOf(_sut.GeneratePurchaseOrder(Po(), "Test Vendor", Items));
        Assert.Contains("PO-000006", text);
        Assert.Contains("Test Vendor", text);
        Assert.Contains("Laptop", text);
        Assert.Contains("Mouse", text);
    }

    [Fact]
    public void PurchaseOrder_DraftHasWatermark_ApprovedDoesNot()
    {
        // If PdfPig reads the rotated watermark in a different order on your machine,
        // relax this to Contains("DRAFT") or drop this test.
        var draft = TextOf(_sut.GeneratePurchaseOrder(Po("Draft"), "V", Items));
        var approved = TextOf(_sut.GeneratePurchaseOrder(Po("Approved"), "V", Items));
        Assert.Contains("PENDING", draft);
        Assert.DoesNotContain("PENDING", approved);
    }

    [Fact]
    public void PurchaseOrder_NoDetails_StillGeneratesPdf()
        => Assert.True(IsPdf(_sut.GeneratePurchaseOrder(Po(lines: 0), "Test Vendor", Items)));

    [Fact]
    public void PurchaseOrder_UnknownItem_UsesFallbackName()
    {
        var text = TextOf(_sut.GeneratePurchaseOrder(Po(lines: 1), "Test Vendor",
            new Dictionary<int, string>()));
        Assert.Contains("Item #5", text);
    }

    [Fact]
    public void PurchaseOrder_ManyLines_SpansMultiplePages()
    {
        var pdf = _sut.GeneratePurchaseOrder(Po(lines: 120), "Test Vendor", Items);
        using var doc = PdfDocument.Open(pdf);
        Assert.True(doc.NumberOfPages > 1);
    }

    // ---------- Sales Invoice ----------
    [Theory]
    [InlineData("Draft")]
    [InlineData("Posted")]
    [InlineData("Cancelled")]
    public void SalesInvoice_AllStatuses_ReturnValidPdf(string status)
        => Assert.True(IsPdf(_sut.GenerateSalesInvoice(Invoice(status), "Test Customer", Items)));

    [Fact]
    public void SalesInvoice_ContainsNumberCustomerAndDueDate()
    {
        var text = TextOf(_sut.GenerateSalesInvoice(Invoice(), "Test Customer", Items));
        Assert.Contains("INV-000003", text);
        Assert.Contains("Test Customer", text);
        Assert.Contains("Due Date", text);
    }

    [Fact]
    public void SalesInvoice_NoDueDateOrNumber_StillGeneratesPdf()
    {
        var inv = Invoice("Draft");
        inv.DueDate = null;
        inv.InvoiceNumber = null;
        Assert.True(IsPdf(_sut.GenerateSalesInvoice(inv, "Test Customer", Items)));
    }

    // ---------- Receipt ----------
    [Theory]
    [InlineData("Draft")]
    [InlineData("Posted")]
    [InlineData("Cancelled")]
    public void Receipt_AllStatuses_ReturnValidPdf(string status)
        => Assert.True(IsPdf(_sut.GenerateReceipt(Receipt(status), "Test Vendor", "PO-000006", Items)));

    [Fact]
    public void Receipt_ContainsPoNumberAndVendor()
    {
        var text = TextOf(_sut.GenerateReceipt(Receipt(), "Test Vendor", "PO-000006", Items));
        Assert.Contains("GRN-000004", text);
        Assert.Contains("PO-000006", text);
        Assert.Contains("Test Vendor", text);
    }

    [Fact]
    public void Receipt_NullNumber_StillGeneratesPdf()
        => Assert.True(IsPdf(_sut.GenerateReceipt(Receipt(number: null), "V", "PO-000006", Items)));
}