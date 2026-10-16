using IMS.MCP.Server.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace IMS.MCP.Server.Tools;

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

    [McpServerTool(Name = "get_customer_counts", ReadOnly = true)]
    [Description("Returns how many customers are active (and not deleted) versus inactive or deleted. " +
                 "Use this for 'how many customers' questions instead of listing customers.")]
    public async Task<string> GetCustomerCounts(
        [Description(InvoiceApiClient.TokenHelp)] string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        // Counting happens here so the AI gets 3 numbers, not the whole customer table.
        var json = await api.GetAsync("api/v1/Customer/GetAll", accessToken, cancellationToken);

        using var doc = JsonDocument.Parse(json);

        if (!TryGet(doc.RootElement, "data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw new McpException("Unexpected response shape from the Customer API.");

        int active = 0, inactive = 0;
        foreach (var customer in data.EnumerateArray())
        {
            var isActive = TryGet(customer, "isActive", out var a) && a.ValueKind == JsonValueKind.True;
            var isDeleted = TryGet(customer, "isDeleted", out var d) && d.ValueKind == JsonValueKind.True;

            if (isActive && !isDeleted) active++; else inactive++;
        }

        return JsonSerializer.Serialize(new
        {
            activeNonDeleted = active,
            inactiveOrDeleted = inactive,
            total = active + inactive
        });
    }

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in element.EnumerateObject())
            {
                if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
