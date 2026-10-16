using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

[McpServerToolType]
public sealed class VendorTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_vendors_paged", ReadOnly = true)]
    [Description("Searches vendors by code, name, mobile or city, with paging.")]
    public Task<string> GetVendorsPaged(
        [Description("Vendor code (optional)")] string? vendorCode = null,
        [Description("Vendor name (optional)")] string? vendorName = null,
        [Description("Mobile number (optional)")] string? mobileNo = null,
        [Description("City (optional)")] string? city = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/Vendor/GetAllPaged",
                ("VendorCode", vendorCode), ("VendorName", vendorName),
                ("MobileNo", mobileNo), ("City", city),
                ("pageNumber", page), ("pageSize", size)),
            accessToken, cancellationToken);
    }

    [McpServerTool(Name = "get_vendor_by_id", ReadOnly = true)]
    [Description("Retrieves one vendor by numeric Id.")]
    public Task<string> GetVendorById(
        [Description("Vendor Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/Vendor/GetById/{id}", accessToken, cancellationToken);
}
