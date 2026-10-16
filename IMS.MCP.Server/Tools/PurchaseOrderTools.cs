using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

// Read-only on purpose. Approve/Cancel are business actions and are NOT exposed yet.
[McpServerToolType]
public sealed class PurchaseOrderTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_purchase_orders_paged", ReadOnly = true)]
    [Description("Searches purchase orders by PO number, vendor Id and status, with paging.")]
    public Task<string> GetPurchaseOrdersPaged(
        [Description("PO number (optional)")] string? poNumber = null,
        [Description("Vendor Id (optional)")] int? vendorId = null,
        [Description("Status, e.g. Draft, Approved, Cancelled (optional)")] string? status = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/PurchaseOrder/GetAllPaged",
                ("PONumber", poNumber), ("VendorId", vendorId), ("Status", status),
                ("pageNumber", page), ("pageSize", size)),
            accessToken, cancellationToken);
    }

    [McpServerTool(Name = "get_purchase_order_by_id", ReadOnly = true)]
    [Description("Retrieves one purchase order (with its details) by numeric Id.")]
    public Task<string> GetPurchaseOrderById(
        [Description("Purchase order Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/PurchaseOrder/GetById/{id}", accessToken, cancellationToken);
}
