using IMS.Blazor.Models;

namespace IMS.Blazor.Services;

/// <summary>Purchase Order: the master calls plus Approve and Cancel.</summary>
public class PurchaseOrderService : MasterService<PurchaseOrder>
{
    public PurchaseOrderService(ApiClient api) : base(api, "PurchaseOrder") { }

    /// <summary>Draft -> Approved</summary>
    public Task<ApiResult> ApproveAsync(int id) => Api.PostAsync($"{BasePath}/Approve/{id}");

    /// <summary>Draft / Approved -> Cancelled (blocked while receipts exist)</summary>
    public Task<ApiResult> CancelAsync(int id) => Api.PostAsync($"{BasePath}/Cancel/{id}");
}

/// <summary>Receipt (goods receipt against a PO): the master calls plus Post and Cancel.</summary>
public class ReceiptService : MasterService<Receipt>
{
    public ReceiptService(ApiClient api) : base(api, "Receipt") { }

    /// <summary>Draft -> Posted: updates PO received quantities, PO status and stock.</summary>
    public Task<ApiResult> PostAsync(int id) => Api.PostAsync($"{BasePath}/Post/{id}");

    /// <summary>Draft / Posted -> Cancelled (blocked when the received stock was already sold).</summary>
    public Task<ApiResult> CancelAsync(int id) => Api.PostAsync($"{BasePath}/Cancel/{id}");
}

/// <summary>Sales Invoice: the master calls plus Post and Cancel.</summary>
public class SalesInvoiceService : MasterService<SalesInvoice>
{
    public SalesInvoiceService(ApiClient api) : base(api, "SalesInvoice") { }

    /// <summary>Draft -> Posted: the stock is checked and reduced.</summary>
    public Task<ApiResult> PostAsync(int id) => Api.PostAsync($"{BasePath}/Post/{id}");

    /// <summary>Draft / Posted -> Cancelled (posted stock is given back).</summary>
    public Task<ApiResult> CancelAsync(int id) => Api.PostAsync($"{BasePath}/Cancel/{id}");
}

/// <summary>Read-only stock view.</summary>
public class StockService
{
    private readonly ApiClient _api;

    public StockService(ApiClient api) => _api = api;

    public async Task<List<ItemStock>> GetAllAsync()
    {
        var result = await _api.GetAsync("v1/Stock/GetAll");

        if (!result.Success)
        {
            throw new ApiException(result.MessageOr("Unable to load the stock."));
        }

        return result.AsList<ItemStock>();
    }
}

/// <summary>Downloads the PDF of a Purchase Order, Receipt or Sales Invoice.</summary>
public class PdfService
{
    private readonly ApiClient _api;

    public PdfService(ApiClient api) => _api = api;

    public Task<byte[]> GetPdfAsync(PdfDocType type, int id) =>
        _api.GetBytesAsync($"v1/{type}/{id}/pdf");
}
