using System.Text.Json;
using System.Text.Json.Serialization;
using Invoice.RAG.Configuration;
using Invoice.RAG.Models;
using Invoice.RAG.Policies;
using Invoice.RAG.Retrieval;
using Invoice.RAG.Services;
using Microsoft.Extensions.Options;

namespace Invoice.RAG.Agent;

/// <summary>
/// The tools the model may call. Structured tools read business data through CoreAPI (the caller's JWT is forwarded,
/// so CoreAPI still decides what this user may see) and do all counting, summing and date maths in C#.
/// Unstructured tools search the uploaded agreements (PDF and Word) and return numbered excerpts for citation.
/// One instance per request: it numbers the excerpts [1], [2]... across all search calls of that request.
/// </summary>
public sealed class AgentToolbox(
    CoreApiClient coreApi,
    Retriever retriever,
    IAgreementStore store,
    AgreementTermsService termsService,
    PolicyIndex policies,
    IOptions<RagOptions> options)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly List<SourceRef> _sources = [];
    private readonly Dictionary<int, int> _chunkRef = [];
    private readonly Dictionary<int, AgreementDocument?> _docs = [];

    public IReadOnlyList<SourceRef> Sources => _sources;

    // ------------------------------------------------------------------ tool catalogue (sent to the model)

    public static IReadOnlyList<object> Definitions { get; } =
    [
        // ---- structured (business data) ----
        Tool("find_vendor",
            "Find vendors by name or vendor code, for example 'VEND004' or 'Northstar'. " +
            "Call this first whenever the user names a vendor, to get its vendorId.",
            new { name = Str("Vendor name or code, or a part of it.") }, ["name"]),

        Tool("get_vendor",
            "Get the code and name of one vendor by vendorId.",
            new { vendorId = Int("The vendor id from find_vendor.") }, ["vendorId"]),

        Tool("list_vendor_purchase_orders",
            "Purchase orders of a vendor: how many, total value per currency, count per status and the 10 newest. " +
            "Use it for questions such as how many POs, total value, or open orders.",
            new
            {
                vendorId = Int("The vendor id from find_vendor."),
                status = Str("Optional. Only count purchase orders with exactly this status.")
            }, ["vendorId"]),

        Tool("get_purchase_order",
            "Get one purchase order (status, date, amount, vendor) by its PO number, for example PO-0001.",
            new { poNumber = Str("The purchase order number.") }, ["poNumber"]),

        Tool("get_payment_summary",
            "Due date, early-payment discount, days past due and late penalty of a purchase order. " +
            "Calculated by the system from the vendor's stored agreement terms. Never calculate these yourself.",
            new
            {
                poNumber = Str("The purchase order number."),
                asOf = Str("Optional date YYYY-MM-DD to calculate for. Default: today.")
            }, ["poNumber"]),

        // ---- company rules (purchase-to-pay skill) ----
        Tool("search_policies",
            "Search the company purchase-to-pay rules (purchase order approval, goods receipt, three-way match tolerance, " +
            "vendor payment terms, discounts, late interest, holds, payment run). Use it for 'what is our rule/policy for...' " +
            "and to decide if an invoice can be paid. A vendor agreement overrides these rules where it states something different.",
            new { query = Str("The rule to look for, in plain words."), topK = Int("Optional. Passages, 1 to 8. Default 4.") }, ["query"]),

        // ---- unstructured (agreement documents) ----
        Tool("list_vendor_agreements",
            "List the agreement documents uploaded for a vendor (id, title, file, effective date).",
            new { vendorId = Int("The vendor id from find_vendor.") }, ["vendorId"]),

        Tool("get_agreement_terms",
            "Payment terms already extracted from a vendor's newest agreement (days, late interest, grace period, " +
            "discount, termination notice, start and end dates). Fast; may be missing or not yet reviewed by a person.",
            new { vendorId = Int("The vendor id from find_vendor.") }, ["vendorId"]),

        Tool("search_vendor_agreements",
            "Search the wording of uploaded vendor agreements (PDF and Word). Use it for clauses: payment terms, late payment, " +
            "delivery, warranty, termination, pricing changes, confidentiality, compliance. " +
            "Leave vendorId empty to search every vendor, for example for 'which vendors have net 45 days'. " +
            "Returns numbered excerpts that must be cited as [ref].",
            new
            {
                query = Str("What to look for, in plain words, for example 'warranty period' or 'late payment interest'."),
                vendorId = Int("Optional. Limit the search to this vendor."),
                agreementId = Int("Optional. Limit the search to this agreement."),
                topK = Int("Optional. Number of excerpts, 1 to 10. Default 5; use 8 to 10 when searching all vendors.")
            }, ["query"])
    ];

    private static object Tool(string name, string description, object properties, string[] required) => new
    {
        type = "function",
        function = new
        {
            name,
            description,
            parameters = new { type = "object", properties, required }
        }
    };

    private static object Str(string description) => new { type = "string", description };
    private static object Int(string description) => new { type = "integer", description };

    // ------------------------------------------------------------------ execution

    /// <summary>Runs one tool and returns its result as JSON text. Problems the model can react to come back as {"error": "..."}.</summary>
    public async Task<string> ExecuteAsync(string name, JsonElement args, CancellationToken ct)
    {
        object result;
        try
        {
            result = name switch
            {
                "find_vendor" => await FindVendorAsync(args, ct),
                "get_vendor" => await GetVendorAsync(args, ct),
                "list_vendor_purchase_orders" => await ListPurchaseOrdersAsync(args, ct),
                "get_purchase_order" => await GetPurchaseOrderAsync(args, ct),
                "get_payment_summary" => await GetPaymentSummaryAsync(args, ct),
                "list_vendor_agreements" => await ListAgreementsAsync(args, ct),
                "get_agreement_terms" => await GetTermsAsync(args, ct),
                "search_vendor_agreements" => await SearchAgreementsAsync(args, ct),
                "search_policies" => await SearchPoliciesAsync(args, ct),
                _ => new { error = $"Unknown tool '{name}'. Use one of the listed tools." }
            };
        }
        catch (ToolArgumentException ex)
        {
            result = new { error = ex.Message };
        }
        catch (RagException ex) when (ex.StatusCode is not (401 or 403 or 503))
        {
            // 401/403/503 (token rejected, not allowed, services down) stop the whole request instead.
            result = new { error = ex.Message };
        }

        return JsonSerializer.Serialize(result, Json);
    }

    // ---- structured tools ----

    private async Task<object> FindVendorAsync(JsonElement args, CancellationToken ct)
    {
        var text = RequiredText(args, "name", "name", "code", "query");
        var vendors = await coreApi.FindVendorsAsync(text, ct);

        return vendors.Count == 0
            ? (object)new { count = 0, note = "No vendor matches. Ask the user for the exact vendor name or code." }
            : new
            {
                count = vendors.Count,
                vendors = vendors.Select(v => new { vendorId = v.Id, code = v.Code, name = v.Name })
            };
    }

    private async Task<object> GetVendorAsync(JsonElement args, CancellationToken ct)
    {
        var vendorId = RequiredInt(args, "vendorId");
        var vendor = await coreApi.GetVendorAsync(vendorId, ct);
        return vendor is null
            ? (object)new { error = $"Vendor {vendorId} was not found." }
            : new { vendorId = vendor.Id, code = vendor.Code, name = vendor.Name };
    }

    private async Task<object> ListPurchaseOrdersAsync(JsonElement args, CancellationToken ct)
    {
        var vendorId = RequiredInt(args, "vendorId");
        var status = JsonPick.Str(args, "status")?.Trim();

        var all = await coreApi.GetPurchaseOrdersByVendorAsync(vendorId, ct);
        var pos = string.IsNullOrEmpty(status)
            ? all
            : all.Where(p => string.Equals(p.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();

        return new
        {
            vendorId,
            statusFilter = string.IsNullOrEmpty(status) ? null : status,
            count = pos.Count,
            countByStatus = pos.GroupBy(p => p.Status ?? "unknown").ToDictionary(g => g.Key, g => g.Count()),
            totalAmountByCurrency = pos.Where(p => p.Amount is not null)
                .GroupBy(p => p.Currency ?? "")
                .Select(g => new { currency = g.Key, total = g.Sum(p => p.Amount!.Value) }),
            newest = pos.OrderByDescending(p => p.PoDate).ThenByDescending(p => p.PoNumber).Take(10)
                .Select(p => new { p.PoNumber, p.Status, p.PoDate, p.Amount, p.Currency }),
            note = all.Count >= 200 ? "Only the first 200 purchase orders were read, so counts and totals may be incomplete." : null
        };
    }

    private async Task<object> GetPurchaseOrderAsync(JsonElement args, CancellationToken ct)
    {
        var poNumber = RequiredText(args, "poNumber", "poNumber", "poNo");
        var po = await coreApi.GetPurchaseOrderAsync(poNumber, ct);
        return po is null ? (object)new { error = $"Purchase order '{poNumber}' was not found." } : po;
    }

    private async Task<object> GetPaymentSummaryAsync(JsonElement args, CancellationToken ct)
    {
        var poNumber = RequiredText(args, "poNumber", "poNumber", "poNo");
        var asOf = DateOnly.TryParse(JsonPick.Str(args, "asOf"), out var d) ? d : DateOnly.FromDateTime(DateTime.UtcNow);

        var po = await coreApi.GetPurchaseOrderAsync(poNumber, ct);
        if (po is null) return new { error = $"Purchase order '{poNumber}' was not found." };

        var effective = await termsService.FindEffectiveAsync(po.VendorId, null, ct);
        if (effective is null)
            return new
            {
                error = "No agreement terms are stored for this vendor yet, so the due date cannot be calculated. " +
                        "Use search_vendor_agreements for the payment-terms wording."
            };

        var (agreement, stored) = effective.Value;
        var summary = PaymentSummaryCalculator.Calculate(po, agreement.Id, stored.Terms, stored.Reviewed, asOf, null);

        return new { purchaseOrder = po, agreementId = agreement.Id, agreementTitle = agreement.Title, summary };
    }

    // ---- unstructured tools ----

    private async Task<object> ListAgreementsAsync(JsonElement args, CancellationToken ct)
    {
        var vendorId = RequiredInt(args, "vendorId");
        var docs = await store.ListAsync(vendorId, ct);

        return new
        {
            vendorId,
            count = docs.Count,
            agreements = docs.Select(a => new { agreementId = a.Id, a.Title, a.FileName, a.EffectiveDate, a.PageCount })
        };
    }

    private async Task<object> GetTermsAsync(JsonElement args, CancellationToken ct)
    {
        var vendorId = RequiredInt(args, "vendorId");
        var effective = await termsService.FindEffectiveAsync(vendorId, null, ct);
        if (effective is null)
            return new { error = "No extracted terms for this vendor. Use search_vendor_agreements to read the agreement wording." };

        var (agreement, stored) = effective.Value;
        var t = stored.Terms;
        return new
        {
            agreementId = agreement.Id,
            agreementTitle = agreement.Title,
            reviewedByPerson = stored.Reviewed,
            terms = new
            {
                t.PaymentTermsDays,
                t.PaymentTermsText,
                t.DueDateBasis,
                t.EarlyPaymentDiscountPercent,
                t.EarlyPaymentDiscountDays,
                t.GracePeriodDays,
                t.LatePenaltyPercent,
                t.LatePenaltyPeriod,
                t.LatePenaltyCapPercent,
                t.DeliveryPenaltyText,
                t.TerminationNoticeDays,
                t.AgreementStartDate,
                t.AgreementEndDate
            }
        };
    }

    private async Task<object> SearchAgreementsAsync(JsonElement args, CancellationToken ct)
    {
        var query = RequiredText(args, "query", "query");
        var vendorId = JsonPick.Int(args, "vendorId");
        var agreementId = JsonPick.Int(args, "agreementId");
        if (vendorId <= 0) vendorId = null;
        if (agreementId <= 0) agreementId = null;
        var topK = Math.Clamp(JsonPick.Int(args, "topK") ?? 5, 1, 10);

        var hits = await retriever.SearchAsync([query], vendorId, agreementId, topK, options.Value.MinScore, ct);
        if (hits.Count == 0)
            return new { excerpts = Array.Empty<object>(), note = "Nothing relevant was found in the uploaded agreements." };

        var excerpts = new List<object>();
        foreach (var h in hits.Take(topK))
        {
            var doc = await GetDocAsync(h.Chunk.AgreementId, ct);
            excerpts.Add(new
            {
                @ref = RefFor(h),
                vendorId = doc?.VendorId,
                agreementId = h.Chunk.AgreementId,
                agreement = h.AgreementTitle,
                page = h.Chunk.Page,
                section = h.Chunk.Section,
                score = Math.Round(h.Score, 3),
                text = h.Chunk.Content
            });
        }

        return new { excerpts };
    }

    private async Task<object> SearchPoliciesAsync(JsonElement args, CancellationToken ct)
    {
        var query = RequiredText(args, "query", "query");
        var topK = Math.Clamp(JsonPick.Int(args, "topK") ?? 4, 1, 8);
        var hits = await policies.SearchAsync(query, topK, ct);

        return hits.Count == 0
            ? (object)new { passages = Array.Empty<object>(), note = "No matching company rule was found." }
            : new { passages = hits.Select(h => new { rule = h.Skill + "/" + h.File, section = h.Section, score = h.Score, text = h.Text }) };
    }

    // ------------------------------------------------------------------ helpers

    private async Task<AgreementDocument?> GetDocAsync(int agreementId, CancellationToken ct)
    {
        if (!_docs.TryGetValue(agreementId, out var doc))
            _docs[agreementId] = doc = await store.GetAsync(agreementId, ct);
        return doc;
    }

    /// <summary>The same chunk always gets the same [n] within one request.</summary>
    private int RefFor(RetrievedChunk h)
    {
        if (_chunkRef.TryGetValue(h.Chunk.Id, out var n)) return n;

        n = _sources.Count + 1;
        _chunkRef[h.Chunk.Id] = n;

        var text = h.Chunk.Content;
        _sources.Add(new SourceRef(n, h.Chunk.AgreementId, h.AgreementTitle, h.Chunk.Page, h.Chunk.Section,
            Math.Round(h.Score, 3), text.Length <= 400 ? text : text[..400] + "..."));
        return n;
    }

    private static string RequiredText(JsonElement args, string label, params string[] names)
    {
        var s = JsonPick.Str(args, names)?.Trim();
        if (string.IsNullOrEmpty(s)) throw new ToolArgumentException($"'{label}' is required.");
        return s.Length > 500 ? s[..500] : s;
    }

    private static int RequiredInt(JsonElement args, string name)
    {
        var v = JsonPick.Int(args, name);
        if (v is null or <= 0) throw new ToolArgumentException($"'{name}' is required and must be a positive number. Call find_vendor first if you only know the vendor name.");
        return v.Value;
    }

    private sealed class ToolArgumentException(string message) : Exception(message);
}
