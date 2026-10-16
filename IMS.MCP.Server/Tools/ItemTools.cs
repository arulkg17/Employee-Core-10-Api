using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

[McpServerToolType]
public sealed class ItemTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_items_paged", ReadOnly = true)]
    [Description("Searches inventory items by category, barcode, code, name, unit of measure and active status, with paging.")]
    public Task<string> GetItemsPaged(
        [Description("Category Id (optional)")] int? categoryId = null,
        [Description("Item barcode (optional)")] string? itemBarCode = null,
        [Description("Item code (optional)")] string? itemCode = null,
        [Description("Item name, partial match depends on the API (optional)")] string? itemName = null,
        [Description("Unit of measure, e.g. KG, PCS (optional)")] string? uom = null,
        [Description("true = only active items, false = only inactive; omit for both")] bool? isActive = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/Itemmaster/GetAllPaged",
                ("CategoryId", categoryId),
                ("ItemBarCode", itemBarCode),
                ("ItemCode", itemCode),
                ("ItemName", itemName),
                ("Uom", uom),
                ("IsActive", isActive?.ToString().ToLowerInvariant()),
                ("PageNumber", page),
                ("PageSize", size)),
            accessToken, cancellationToken);
    }

    [McpServerTool(Name = "get_item_by_id", ReadOnly = true)]
    [Description("Retrieves one inventory item by numeric Id.")]
    public Task<string> GetItemById(
        [Description("Item Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/Itemmaster/GetById/{id}", accessToken, cancellationToken);

    [McpServerTool(Name = "get_active_item_count_by_category", ReadOnly = true)]
    [Description("Returns the number of active items in a category (by category Id).")]
    public Task<string> GetActiveItemCountByCategory(
        [Description("Category Id")] int categoryId,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/Itemmaster/GetActiveItemCountByCategory/{categoryId}", accessToken, cancellationToken);
}
