using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

// Customer counts live in CountTools (get_customer_counts, get_customer_counts_by_city).
[McpServerToolType]
public sealed class CustomerTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_customers_paged", ReadOnly = true)]
    [Description("Searches customers by code, name, mobile or city, with paging.")]
    public Task<string> GetCustomersPaged(
        [Description("Customer code (optional)")] string? customerCode = null,
        [Description("Customer name (optional)")] string? customerName = null,
        [Description("Mobile number (optional)")] string? mobileNo = null,
        [Description("City (optional)")] string? city = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/Customer/GetAllPaged",
                ("CustomerCode", customerCode), ("CustomerName", customerName),
                ("MobileNo", mobileNo), ("City", city),
                ("pageNumber", page), ("pageSize", size)),
            accessToken, cancellationToken);
    }

    [McpServerTool(Name = "get_customer_by_id", ReadOnly = true)]
    [Description("Retrieves one customer by numeric Id.")]
    public Task<string> GetCustomerById(
        [Description("Customer Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/Customer/GetById/{id}", accessToken, cancellationToken);
}
