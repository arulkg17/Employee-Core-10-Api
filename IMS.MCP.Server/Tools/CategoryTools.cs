using IMS.MCP.Server.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace IMS.MCP.Server.Tools;

[McpServerToolType]
public sealed class CategoryTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_categories", ReadOnly = true)]
    [Description("Retrieves all item categories from the IMS Invoice API.")]
    public Task<string> GetCategories(
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync("api/v1/Category/GetAll", accessToken, cancellationToken);

    [McpServerTool(Name = "get_category_by_id", ReadOnly = true)]
    [Description("Retrieves a single category by its numeric Id.")]
    public Task<string> GetCategoryById(
        [Description("Category Id")] int id,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync($"api/v1/Category/GetById/{id}", accessToken, cancellationToken);

    [McpServerTool(Name = "get_categories_paged", ReadOnly = true)]
    [Description("Searches categories with optional code/name filters and paging.")]
    public Task<string> GetCategoriesPaged(
        [Description("Filter by category code (optional)")] string? code = null,
        [Description("Filter by category name (optional)")] string? name = null,
        [Description("Page number, starting at 1")] int pageNumber = 1,
        [Description("Page size, 1-50")] int pageSize = 10,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        var (page, size) = InvoiceApiClient.ClampPaging(pageNumber, pageSize);

        return api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/Category/GetAllPaged",
                ("Code", code), ("Name", name), ("pageNumber", page), ("pageSize", size)),
            accessToken, cancellationToken);
    }
}
