using Invoice.BAL.Contracts;

public class DocumentPrintService : IDocumentPrintService
{
    private readonly IPurchaseOrderService _po;
    private readonly ISalesInvoiceService _si;
    private readonly IReceiptService _rc;
    private readonly IVendorService _vendors;
    private readonly ICustomerService _customers;
    private readonly IItemmasterService _items;
    private readonly IDocumentPdfService _pdf;

    public DocumentPrintService(IPurchaseOrderService po, ISalesInvoiceService si,
        IReceiptService rc, IVendorService vendors, ICustomerService customers,
        IItemmasterService items, IDocumentPdfService pdf)
    {
        _po = po; _si = si; _rc = rc; _vendors = vendors; _customers = customers;
        _items = items; _pdf = pdf;
    }

    public async Task<byte[]?> GetPurchaseOrderPdfAsync(int id)
    {
        var po = await _po.GetByIdAsync(id);
        if (po == null) return null;
        var vendor = await _vendors.GetByIdAsync(po.VendorId);
        var items = await ItemNamesAsync(po.Details.Select(d => d.ItemmasterId));
        return _pdf.GeneratePurchaseOrder(po, vendor?.VendorName ?? $"Vendor #{po.VendorId}", items);
    }

    public async Task<byte[]?> GetSalesInvoicePdfAsync(int id)
    {
        var inv = await _si.GetByIdAsync(id);
        if (inv == null) return null;
        var cust = await _customers.GetByIdAsync(inv.CustomerId);
        var items = await ItemNamesAsync(inv.Details.Select(d => d.ItemmasterId));
        return _pdf.GenerateSalesInvoice(inv, cust?.CustomerName ?? $"Customer #{inv.CustomerId}", items);
    }

    public async Task<byte[]?> GetReceiptPdfAsync(int id)
    {
        var rc = await _rc.GetByIdAsync(id);
        if (rc == null) return null;
        var po = await _po.GetByIdAsync(rc.PurchaseOrderId);
        var vendor = await _vendors.GetByIdAsync(rc.VendorId);
        var items = await ItemNamesAsync(rc.Details.Select(d => d.ItemmasterId));
        return _pdf.GenerateReceipt(rc, vendor?.VendorName ?? $"Vendor #{rc.VendorId}",
                                    po?.PONumber ?? $"PO #{rc.PurchaseOrderId}", items);
    }

    private async Task<Dictionary<int, string>> ItemNamesAsync(IEnumerable<int> ids)
    {
        var map = new Dictionary<int, string>();
        foreach (var id in ids.Distinct())
        {
            var item = await _items.GetByIdAsync(id);
            map[id] = item?.ItemName ?? $"Item #{id}";
        }
        return map;
    }
}