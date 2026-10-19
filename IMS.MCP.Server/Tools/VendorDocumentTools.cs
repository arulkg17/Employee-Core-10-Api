using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using IMS.MCP.Server.Clients;
using ModelContextProtocol.Server;

namespace IMS.MCP.Server.Tools;

/// <summary>
/// UNSTRUCTURED vendor data: the wording of uploaded vendor agreements (PDF and Word), kept in the InvoiceRag database
/// and read through the Invoice.RAG API. Search results are returned with vendor, page and section so answers can cite them.
/// </summary>
[McpServerToolType]
public sealed class VendorDocumentTools(ImsApi api)
{
    [McpServerTool(Name = "search_vendor_agreements"),
     Description("Search the wording of uploaded vendor agreements (PDF and Word). Use it for clauses: payment terms, late payment " +
                 "interest, delivery, warranty, termination, pricing changes, insurance, confidentiality, compliance. " +
                 "Leave vendorId empty to search every vendor, for example for 'which vendors have net 45 days'. " +
                 "Returns numbered excerpts: cite them by agreement title and page.")]
    public Task<string> SearchVendorAgreements(
        [Description("What to look for, in plain words, for example 'warranty period' or 'late payment interest'.")] string query,
        [Description("Optional. Limit the search to this vendorId (from find_vendor).")] int? vendorId = null,
        [Description("Optional. Limit the search to one agreement id.")] int? agreementId = null,
        [Description("Optional. Number of excerpts, 1 to 10. Default 5; use 8 to 10 when searching all vendors.")] int? topK = null,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var text = query?.Trim() ?? "";
            if (text.Length == 0) return new { error = "'query' is required." };

            var body = new
            {
                query = text,
                vendorId = vendorId > 0 ? vendorId : null,
                agreementId = agreementId > 0 ? agreementId : null,
                topK = Math.Clamp(topK ?? 5, 1, 10)
            };

            var data = await api.PostAsync(ImsApi.Rag, api.Path("RagSearch", "api/v1/rag/search"), body, ct);

            var excerpts = new List<object>();
            if (data is not null)
            {
                foreach (var hit in Js.Rows(data.Value))
                {
                    var score = Js.Dbl(hit, "score") ?? 0;
                    if (score < api.MinScore) continue;   // weak matches are noise in an all-vendor search

                    excerpts.Add(new
                    {
                        @ref = excerpts.Count + 1,
                        vendorId = Js.Int(hit, "vendorId"),
                        agreementId = Js.Int(hit, "agreementId"),
                        agreement = Js.Str(hit, "agreementTitle"),
                        page = Js.Int(hit, "page"),
                        section = Js.Str(hit, "section"),
                        score,
                        text = Js.Str(hit, "text")
                    });
                }
            }

            return excerpts.Count == 0
                ? new { excerpts, note = "Nothing relevant was found in the uploaded agreements." }
                : new { excerpts, note = (string?)null };
        });

    [McpServerTool(Name = "list_vendor_agreements"),
     Description("List the agreement documents uploaded for a vendor (agreementId, title, file, effective date, pages).")]
    public Task<string> ListVendorAgreements(
        [Description("The vendorId from find_vendor.")] int vendorId,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            if (vendorId <= 0) return new { error = "'vendorId' must be a positive number. Call find_vendor first." };

            var agreements = await ListAsync(vendorId, ct);
            return new
            {
                vendorId,
                count = agreements.Count,
                agreements = agreements.Select(a => new
                {
                    agreementId = Js.Int(a, "id"),
                    title = Js.Str(a, "title"),
                    fileName = Js.Str(a, "fileName"),
                    effectiveDate = Js.Str(a, "effectiveDate"),
                    pages = Js.Int(a, "pageCount")
                })
            };
        });

    [McpServerTool(Name = "get_agreement_terms"),
     Description("Payment terms already extracted from a vendor's newest agreement: payment days, late interest, grace period, " +
                 "early-payment discount, termination notice, start and end dates. Fast, but may be missing or not yet reviewed " +
                 "by a person. For anything else use search_vendor_agreements.")]
    public Task<string> GetAgreementTerms(
        [Description("The vendorId from find_vendor.")] int vendorId,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            if (vendorId <= 0) return new { error = "'vendorId' must be a positive number. Call find_vendor first." };

            foreach (var agreement in await ListAsync(vendorId, ct))   // newest first
            {
                var id = Js.Int(agreement, "id");
                if (id is null) continue;

                var url = api.Path("AgreementTerms", "api/v1/vendor-agreements/{id}/terms")
                    .Replace("{id}", id.Value.ToString(CultureInfo.InvariantCulture));
                var data = await api.GetAsync(ImsApi.Rag, url, allowNotFound: true, ct);
                if (data is null) continue;

                var t = Js.Find(data.Value, "terms") ?? default;
                return new
                {
                    agreementId = id,
                    agreement = Js.Str(agreement, "title"),
                    reviewedByPerson = Js.Bool(data.Value, "reviewed"),
                    terms = new
                    {
                        paymentTermsDays = Js.Int(t, "paymentTermsDays"),
                        paymentTermsText = Js.Str(t, "paymentTermsText"),
                        dueDateBasis = Js.Str(t, "dueDateBasis"),
                        earlyPaymentDiscountPercent = Js.Dec(t, "earlyPaymentDiscountPercent"),
                        earlyPaymentDiscountDays = Js.Int(t, "earlyPaymentDiscountDays"),
                        gracePeriodDays = Js.Int(t, "gracePeriodDays"),
                        latePenaltyPercent = Js.Dec(t, "latePenaltyPercent"),
                        latePenaltyPeriod = Js.Str(t, "latePenaltyPeriod"),
                        latePenaltyCapPercent = Js.Dec(t, "latePenaltyCapPercent"),
                        terminationNoticeDays = Js.Int(t, "terminationNoticeDays"),
                        agreementStartDate = Js.Str(t, "agreementStartDate"),
                        agreementEndDate = Js.Str(t, "agreementEndDate")
                    }
                };
            }

            return new
            {
                error = "No extracted terms for this vendor. Use search_vendor_agreements to read the agreement wording " +
                        "(terms are created by POST api/v1/vendor-agreements/{id}/extract-terms)."
            };
        });

    [McpServerTool(Name = "get_po_payment_summary"),
     Description("Due date, early-payment discount, days past due and estimated late penalty for a purchase order. " +
                 "Uses the PO from the Accounts database and the vendor's stored agreement terms; the dates and money are " +
                 "calculated by the system, so never calculate them yourself.")]
    public Task<string> GetPoPaymentSummary(
        [Description("The purchase order number, for example PO-0001.")] string poNumber,
        [Description("Optional date YYYY-MM-DD to calculate for. Default: today.")] string? asOf = null,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var number = poNumber?.Trim() ?? "";
            if (number.Length == 0) return new { error = "'poNumber' is required." };

            var url = api.Path("PoPaymentSummary", "api/v1/rag/po/{po}/payment-summary")
                .Replace("{po}", Uri.EscapeDataString(number));
            if (DateOnly.TryParse(asOf, out var date))
                url += "?asOf=" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var data = await api.GetAsync(ImsApi.Rag, url, allowNotFound: true, ct);
            return data is null
                ? new { error = $"Purchase order '{number}' was not found, or its vendor has no extracted agreement terms yet." } as object
                : data.Value;
        });

    [McpServerTool(Name = "ask_vendor_questions"),
     Description("Ask one or several vendor questions at once and get one written answer. The Invoice.RAG service decides by itself " +
                 "which structured and agreement data to read, using its own local model, so it is slow. " +
                 "Prefer the individual tools above when you can combine their results yourself.")]
    public Task<string> AskVendorQuestions(
        [Description("The questions, one per item.")] string[] questions,
        CancellationToken ct = default)
        => ToolRunner.RunAsync(async () =>
        {
            var list = (questions ?? []).Where(q => !string.IsNullOrWhiteSpace(q)).Select(q => q.Trim()).ToList();
            if (list.Count == 0) return new { error = "Provide at least one question." };

            var data = await api.PostAsync(ImsApi.Rag, api.Path("AgentAsk", "api/v1/agent/ask"), new { questions = list }, ct);
            return data is null ? new { error = "The service returned no answer." } as object : data.Value;
        });

    // ------------------------------------------------------------------ helpers

    /// <summary>Agreements of one vendor, newest first (the order the Invoice.RAG API returns them in).</summary>
    private async Task<List<JsonElement>> ListAsync(int vendorId, CancellationToken ct)
    {
        var url = api.Path("AgreementsByVendor", "api/v1/vendor-agreements?vendorId={vendorId}")
            .Replace("{vendorId}", vendorId.ToString(CultureInfo.InvariantCulture));
        var data = await api.GetAsync(ImsApi.Rag, url, allowNotFound: true, ct);
        return data is null ? [] : Js.Rows(data.Value).ToList();
    }
}
