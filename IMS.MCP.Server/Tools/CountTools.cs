using IMS.MCP.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace IMS.MCP.Server.Tools;

/// <summary>
/// Count tools. The counting rules live in the BAL (IMasterCountService); this class only calls the
/// MasterCount API endpoints, so Copilot, Invoice.AI and every other client get identical numbers.
/// Result shape: { active, inactive, total } - deleted rows never count.
/// </summary>
[McpServerToolType]
public sealed class CountTools(InvoiceApiClient api)
{
    [McpServerTool(Name = "get_customer_counts", ReadOnly = true)]
    [Description("Returns how many customers are active versus inactive (deleted customers are never counted). " +
                 "Use this for 'how many customers' questions instead of listing customers.")]
    public Task<string> GetCustomerCounts(
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => SummaryAsync("api/v1/MasterCount/Customers", null, accessToken, cancellationToken);

    [McpServerTool(Name = "get_vendor_counts", ReadOnly = true)]
    [Description("Returns how many vendors are active versus inactive.")]
    public Task<string> GetVendorCounts(
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => SummaryAsync("api/v1/MasterCount/Vendors", null, accessToken, cancellationToken);

    [McpServerTool(Name = "get_category_counts", ReadOnly = true)]
    [Description("Returns how many categories are active versus inactive.")]
    public Task<string> GetCategoryCounts(
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => SummaryAsync("api/v1/MasterCount/Categories", null, accessToken, cancellationToken);

    [McpServerTool(Name = "get_item_counts", ReadOnly = true)]
    [Description("Returns how many inventory items are active versus inactive, optionally for one category Id.")]
    public Task<string> GetItemCounts(
        [Description("Category Id (optional)")] int? categoryId = null,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => SummaryAsync("api/v1/MasterCount/Items", categoryId, accessToken, cancellationToken);

    [McpServerTool(Name = "get_customer_counts_by_city", ReadOnly = true)]
    [Description("Returns the number of customers per city. isActive: true = active only, false = inactive only, omit = all.")]
    public Task<string> GetCustomerCountsByCity(
        [Description("true, false, or omit for all")] bool? isActive = null,
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
        => api.GetAsync(
            InvoiceApiClient.WithQuery("api/v1/MasterCount/CustomersByCity",
                ("isActive", isActive?.ToString().ToLowerInvariant())),
            accessToken, cancellationToken);

    // ------------------------------------------------------------------

    private async Task<string> SummaryAsync(string path, int? categoryId, string? accessToken, CancellationToken ct)
    {
        var active = await ReadCountAsync(path, categoryId, true, accessToken, ct);
        var inactive = await ReadCountAsync(path, categoryId, false, accessToken, ct);

        return JsonSerializer.Serialize(new { active, inactive, total = active + inactive });
    }

    private async Task<int> ReadCountAsync(string path, int? categoryId, bool isActive, string? accessToken, CancellationToken ct)
    {
        var json = await api.GetAsync(
            InvoiceApiClient.WithQuery(path,
                ("isActive", isActive.ToString().ToLowerInvariant()),
                ("categoryId", categoryId)),
            accessToken, ct);

        using var doc = JsonDocument.Parse(json);

        foreach (var p in doc.RootElement.EnumerateObject())
            if (p.Name.Equals("data", StringComparison.OrdinalIgnoreCase) && p.Value.ValueKind == JsonValueKind.Number)
                return p.Value.GetInt32();

        throw new McpException("Unexpected response shape from the Invoice API.");
    }
}
