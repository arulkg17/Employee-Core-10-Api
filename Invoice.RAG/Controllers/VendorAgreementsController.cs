using Invoice.RAG.Documents;
using Invoice.RAG.Models;
using Invoice.RAG.Retrieval;
using Invoice.RAG.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.RAG.Controllers;

[ApiController]
[Route("api/v1/vendor-agreements")]
[Authorize]
public class VendorAgreementsController(
    AgreementIngestionService ingestion,
    AgreementTermsService terms,
    IAgreementStore store) : ControllerBase
{
    // POST api/v1/vendor-agreements   (multipart/form-data: file, vendorId, title?, effectiveDate?)
    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] UploadAgreementRequest request, CancellationToken ct)
    {
        var doc = await ingestion.IngestAsync(
            request.File, request.VendorId, request.Title, request.EffectiveDate, User.Identity?.Name, ct);

        return Ok(new ApiResult<AgreementInfo>(true, "Agreement uploaded and indexed.", AgreementInfo.From(doc)));
    }

    // GET api/v1/vendor-agreements?vendorId=1
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? vendorId, CancellationToken ct)
    {
        var docs = await store.ListAsync(vendorId, ct);
        return Ok(new ApiResult<IEnumerable<AgreementInfo>>(true, "Agreements retrieved.", docs.Select(AgreementInfo.From)));
    }

    // GET api/v1/vendor-agreements/3
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var doc = await store.GetAsync(id, ct);
        return doc is null
            ? NotFound(new ApiResult<object>(false, "Agreement not found."))
            : Ok(new ApiResult<AgreementInfo>(true, "Agreement retrieved.", AgreementInfo.From(doc)));
    }

    // GET api/v1/vendor-agreements/3/file   (original PDF or Word file, for "open the source" links in the UI)
    [HttpGet("{id:int}/file")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var doc = await store.GetAsync(id, ct);
        if (doc is null || !System.IO.File.Exists(doc.StoredPath))
            return NotFound(new ApiResult<object>(false, "Agreement file not found."));

        return PhysicalFile(doc.StoredPath, DocumentTextExtractor.ContentTypeFor(doc.StoredPath), doc.FileName);
    }

    // DELETE api/v1/vendor-agreements/3
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await ingestion.DeleteAsync(id, ct);
        return Ok(new ApiResult<object>(true, "Agreement deleted."));
    }

    // POST api/v1/vendor-agreements/3/extract-terms
    [HttpPost("{id:int}/extract-terms")]
    public async Task<IActionResult> ExtractTerms(int id, CancellationToken ct)
        => Ok(new ApiResult<StoredTermsDto>(true, "Terms extracted. Please review them.", await terms.ExtractAsync(id, ct)));

    // GET api/v1/vendor-agreements/3/terms
    [HttpGet("{id:int}/terms")]
    public async Task<IActionResult> GetTerms(int id, CancellationToken ct)
    {
        var result = await terms.GetAsync(id, ct);
        return result is null
            ? NotFound(new ApiResult<object>(false, "No terms yet. Call extract-terms first."))
            : Ok(new ApiResult<StoredTermsDto>(true, "Terms retrieved.", result));
    }

    // PUT api/v1/vendor-agreements/3/terms   (a person corrects the terms; they are then marked as reviewed)
    [HttpPut("{id:int}/terms")]
    public async Task<IActionResult> ReviewTerms(int id, [FromBody] AgreementTerms body, CancellationToken ct)
        => Ok(new ApiResult<StoredTermsDto>(true, "Terms saved and marked as reviewed.",
            await terms.SaveReviewedAsync(id, body, User.Identity?.Name, ct)));
}
