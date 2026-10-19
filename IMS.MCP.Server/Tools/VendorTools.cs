using System.ComponentModel;
using System.Globalization;
using IMS.MCP.Server.Clients;
using ModelContextProtocol.Server;

namespace IMS.MCP.Server.Tools;

/// <summary>
/// STRUCTURED vendor data: vendors and purchase orders from the Accounts database, read through CoreAPI.
/// Counting and summing is done here in C#, never left to the model.
/// </summary>
[McpServerToolType]
public sealed class VendorTools(ImsApi api)
{
    // Possible JSON names of the purchase-order fields (case-insensitive, first match wins). Adjust to your PurchaseOrderDto.
    private static readonly string[] PoNumber = ["poNumber", "poNo"];
    private static readonly string[] PoDate = ["poDate", "orderDate", "date", "createdDate"];
    private static readonly string[] PoVendorId = ["vendorId"];
    private static readonly string[] PoStatus = ["status"];
    private static readonly string[] PoAmount = ["totalAmount", "grandTotal", "netAmount", "total", "amount"];
    private static readonly string[] PoCurrency = ["currency", "currencyCode"];

    [McpServerTool(Name = "find_vendor"),
     Description("Find vendors by name or vendor code, for example 'VEND004' or 'Northstar'. " +
                 "Call this first when a question names a vendor, to get its vendorId.")]
    public Task<string> FindVendor(
        [Description("Vendor name or code, or a part of it.")] string name,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var needle = name?.Trim() ?? "";
            if (needle.Length == 0) return new { error = "'name' is required." };

            var data = await api.GetAsync(ImsApi.Core,
                api.Path("VendorList", "api/v1/Vendor/GetAllPaged?pageNumber=1&pageSize=200"), allowNotFound: true, ct);

            var found = new List<object>();
            if (data is not null)
            {
                foreach (var row in Js.Rows(data.Value))
                {
                    var id = Js.Int(row, "id", "vendorId");
                    if (id is null) continue;
                    var code = Js.Str(row, "vendorCode", "code");
                    var vname = Js.Str(row, "vendorName", "name");

                    if (Has(code, needle) || Has(vname, needle) || Has(needle, vname))
                        found.Add(new { vendorId = id, code, name = vname });
                }
            }

            return found.Count == 0
                ? new { count = 0, note = "No vendor matches. Ask the user for the exact vendor name or code." } as object
                : new { count = found.Count, vendors = found.Take(10) };
        });

    [McpServerTool(Name = "get_vendor"),
     Description("Get the code and name of one vendor by vendorId.")]
    public Task<string> GetVendor(
        [Description("The vendorId from find_vendor.")] int vendorId,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            if (vendorId <= 0) return new { error = "'vendorId' must be a positive number. Call find_vendor first." };

            var url = api.Path("VendorById", "api/v1/Vendor/GetById/{id}")
                .Replace("{id}", vendorId.ToString(CultureInfo.InvariantCulture));
            var data = await api.GetAsync(ImsApi.Core, url, allowNotFound: true, ct);
            if (data is null) return new { error = $"Vendor {vendorId} was not found." };

            var v = data.Value;
            return new
            {
                vendorId,
                code = Js.Str(v, "vendorCode", "code"),
                name = Js.Str(v, "vendorName", "name")
            };
        });

    [McpServerTool(Name = "get_vendor_purchase_orders"),
     Description("Purchase orders of a vendor: how many, total value per currency, count per status and the 10 newest. " +
                 "Use it for questions such as how many POs a vendor has, their total value, or how many are open.")]
    public Task<string> GetVendorPurchaseOrders(
        [Description("The vendorId from find_vendor.")] int vendorId,
        [Description("Optional. Only count purchase orders with exactly this status.")] string? status = null,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            if (vendorId <= 0) return new { error = "'vendorId' must be a positive number. Call find_vendor first." };

            var url = api.Path("PurchaseOrdersByVendor",
                    "api/v1/PurchaseOrder/GetAllPaged?VendorId={vendorId}&pageNumber=1&pageSize=200")
                .Replace("{vendorId}", vendorId.ToString(CultureInfo.InvariantCulture));
            var data = await api.GetAsync(ImsApi.Core, url, allowNotFound: true, ct);

            var all = new List<Po>();
            if (data is not null)
            {
                foreach (var row in Js.Rows(data.Value))
                {
                    var rowVendor = Js.Int(row, PoVendorId);
                    if (rowVendor is not null && rowVendor != vendorId) continue;   // in case the API ignored the filter
                    all.Add(ToPo(row, rowVendor ?? vendorId));
                }
            }

            var wanted = status?.Trim();
            var pos = string.IsNullOrEmpty(wanted)
                ? all
                : all.Where(p => string.Equals(p.Status, wanted, StringComparison.OrdinalIgnoreCase)).ToList();

            return new
            {
                vendorId,
                statusFilter = string.IsNullOrEmpty(wanted) ? null : wanted,
                count = pos.Count,
                countByStatus = pos.GroupBy(p => p.Status ?? "unknown").ToDictionary(g => g.Key, g => g.Count()),
                totalAmountByCurrency = pos.Where(p => p.Amount is not null)
                    .GroupBy(p => p.Currency ?? "")
                    .Select(g => new { currency = g.Key, total = g.Sum(p => p.Amount!.Value) }),
                newest = pos.OrderByDescending(p => p.PoDate).ThenByDescending(p => p.PoNumber).Take(10),
                note = all.Count >= 200 ? "Only the first 200 purchase orders were read, so counts and totals may be incomplete." : null
            };
        });

    [McpServerTool(Name = "get_purchase_order"),
     Description("Get one purchase order (status, date, amount, vendorId) by its PO number, for example PO-0001.")]
    public Task<string> GetPurchaseOrder(
        [Description("The purchase order number.")] string poNumber,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var number = poNumber?.Trim() ?? "";
            if (number.Length == 0) return new { error = "'poNumber' is required." };

            var url = api.Path("PurchaseOrderByNumber",
                    "api/v1/PurchaseOrder/GetAllPaged?PONumber={po}&pageNumber=1&pageSize=20")
                .Replace("{po}", Uri.EscapeDataString(number));
            var data = await api.GetAsync(ImsApi.Core, url, allowNotFound: true, ct);

            if (data is not null)
                foreach (var row in Js.Rows(data.Value))
                {
                    var po = ToPo(row, Js.Int(row, PoVendorId));
                    if (string.Equals(po.PoNumber, number, StringComparison.OrdinalIgnoreCase)) return po;
                }

            return new { error = $"Purchase order '{number}' was not found." };
        });

    // ------------------------------------------------------------------ helpers

    private sealed record Po(string PoNumber, int? VendorId, string? Status, string? PoDate, decimal? Amount, string? Currency);

    private static Po ToPo(System.Text.Json.JsonElement row, int? vendorId)
    {
        var date = Js.Str(row, PoDate);
        if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            date = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return new Po(
            Js.Str(row, PoNumber)?.Trim() ?? "",
            vendorId,
            Js.Str(row, PoStatus),
            date,
            Js.Dec(row, PoAmount),
            Js.Str(row, PoCurrency));
    }

    private static bool Has(string? haystack, string? needle) =>
        !string.IsNullOrWhiteSpace(haystack) && !string.IsNullOrWhiteSpace(needle) &&
        haystack.Contains(needle.Trim(), StringComparison.OrdinalIgnoreCase);
}
