using System.Net;
using System.Text.Json;
using Invoice.RAG.Configuration;
using Invoice.RAG.Models;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Services;

/// <summary>
/// Structured business data (vendors, purchase orders) comes from CoreAPI, never from the database directly,
/// so BAL rules and the user's permissions still apply.
/// </summary>
public sealed class CoreApiClient(HttpClient http, IOptions<RagOptions> options, ILogger<CoreApiClient> logger)
{
    public async Task<VendorFacts?> GetVendorAsync(int vendorId, CancellationToken ct)
    {
        var data = await GetDataAsync($"api/v1/Vendor/GetById/{vendorId}", allowNotFound: true, ct);
        if (data is null) return null;

        var v = data.Value;
        return new VendorFacts(vendorId, JsonPick.Str(v, "vendorCode", "code"), JsonPick.Str(v, "vendorName", "name"));
    }

    public async Task<PoFacts?> GetPurchaseOrderAsync(string poNumber, CancellationToken ct)
    {
        var map = options.Value.PoFields;

        var data = await GetDataAsync(
            $"api/v1/PurchaseOrder/GetAllPaged?PONumber={Uri.EscapeDataString(poNumber)}&pageNumber=1&pageSize=20",
            allowNotFound: false, ct);
        if (data is null) return null;

        foreach (var row in JsonPick.Rows(data.Value))
        {
            var number = JsonPick.Str(row, map.PoNumber);
            if (!string.Equals(number?.Trim(), poNumber.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            var po = new PoFacts
            {
                PoNumber = number!.Trim(),
                Id = JsonPick.Int(row, map.Id),
                VendorId = JsonPick.Int(row, map.VendorId),
                Status = JsonPick.Str(row, map.Status),
                PoDate = JsonPick.Date(row, map.PoDate),
                Amount = JsonPick.Dec(row, map.Amount),
                Currency = JsonPick.Str(row, map.Currency)
            };

            if (po.VendorId is not null)
            {
                var vendor = await GetVendorAsync(po.VendorId.Value, ct);
                po.VendorCode = vendor?.Code;
                po.VendorName = vendor?.Name;
            }

            return po;
        }

        return null;
    }

    private async Task<JsonElement?> GetDataAsync(string url, bool allowNotFound, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await http.GetAsync(url, ct);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "CoreAPI is not reachable for {Url}", url);
            throw new RagException(503, "The business API (CoreAPI) is not reachable.");
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.NotFound && allowNotFound) return null;
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new RagException(401, "CoreAPI rejected the token.");
            if (response.StatusCode == HttpStatusCode.Forbidden) throw new RagException(403, "You are not allowed to read this data in CoreAPI.");
            if (!response.IsSuccessStatusCode) throw new RagException(502, $"CoreAPI returned HTTP {(int)response.StatusCode}.");

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return JsonPick.Find(doc.RootElement, "data")?.Clone();
        }
    }
}
