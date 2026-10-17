using Invoice.RAG.Models;
using Invoice.RAG.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.RAG.Controllers;

[ApiController]
[Route("api/v1/rag")]
[Authorize]
public class RagController(
    RagAnswerService answers,
    AgreementTermsService terms,
    CoreApiClient coreApi) : ControllerBase
{
    // POST api/v1/rag/search   { "query": "late payment penalty", "vendorId": 1 }
    // Retrieval only (no model call): check that the right clauses and pages come back before trusting answers.
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken ct)
        => Ok(new ApiResult<IReadOnlyList<SearchHit>>(true, "Search completed.", await answers.SearchAsync(request, ct)));

    // POST api/v1/rag/ask   { "question": "...", "poNumber": "PO-001" }  or  { "question": "...", "vendorId": 1 }
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
        => Ok(new ApiResult<AskResponse>(true, "Answer generated.", await answers.AskAsync(request, ct)));

    // GET api/v1/rag/po/PO-001/payment-summary?asOf=2026-11-01&invoiceDate=2026-09-05
    // No language model involved: PO facts from CoreAPI + stored agreement terms + C# date/penalty maths.
    [HttpGet("po/{poNumber}/payment-summary")]
    public async Task<IActionResult> GetPaymentSummary(
        string poNumber, [FromQuery] int? agreementId, [FromQuery] DateOnly? asOf, [FromQuery] DateOnly? invoiceDate, CancellationToken ct)
    {
        var po = await coreApi.GetPurchaseOrderAsync(poNumber, ct);
        if (po is null)
            return NotFound(new ApiResult<object>(false, $"Purchase order '{poNumber}' was not found."));

        var effective = await terms.FindEffectiveAsync(po.VendorId, agreementId, ct);
        if (effective is null)
            return NotFound(new ApiResult<object>(false,
                "No agreement terms found for this vendor. Upload the agreement and call extract-terms first."));

        var (agreement, stored) = effective.Value;
        var summary = PaymentSummaryCalculator.Calculate(
            po, agreement.Id, stored.Terms, stored.Reviewed, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow), invoiceDate);

        return Ok(new ApiResult<object>(true, "Payment summary calculated.", new { purchaseOrder = po, summary }));
    }
}
