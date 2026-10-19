using Invoice.RAG.Models;
using Invoice.RAG.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.RAG.Controllers;

[ApiController]
[Route("api/v1/policies")]
[Authorize]
public class PolicyController(PolicyIndex index) : ControllerBase
{
    // GET api/v1/policies/search?query=over-receipt tolerance&topK=4
    // Passages of the company purchase-to-pay rules (skills folder), best match first.
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int topK = 4, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 500)
            return BadRequest(new ApiResult<object>(false, "Provide a query (up to 500 characters).", null));

        var hits = await index.SearchAsync(query.Trim(), topK, ct);
        return Ok(new ApiResult<IReadOnlyList<PolicyHit>>(true, $"{hits.Count} passage(s).", hits));
    }
}
