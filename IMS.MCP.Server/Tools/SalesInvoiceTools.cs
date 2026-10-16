using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

// Read-only on purpose. Post/Cancel change stock, so they are NOT exposed yet.
[McpServerToolType]
public sealed class SalesInvoiceTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_sales_invoices_paged", ReadOnly = true)]
    [Description("Searches sales invoices by invoice number, customer Id and status, with paging.")]
    public Task<string> GetSalesInvoicesPaged(
        [Description("Invoice number (optional)")] string? invoiceNumber = null,
        [Description("Customer Id (optional)")] int? customerId = null,
        [Description("Status, e.g. Draft, Posted, Cancelled (optional)")] string? status = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/SalesInvoice/GetAllPaged",
                ("invoiceNumber", invoiceNumber), ("customerId", customerId), ("status", status),
                ("pageNumber", page), ("pageSize", size)),
            accessToken, cancellationToken);
    }

    [McpServerTool(Name = "get_sales_invoice_by_id", ReadOnly = true)]
    [Description("Retrieves one sales invoice (with its details) by numeric Id.")]
    public Task<string> GetSalesInvoiceById(
        [Description("Sales invoice Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/SalesInvoice/GetById/{id}", accessToken, cancellationToken);
}
