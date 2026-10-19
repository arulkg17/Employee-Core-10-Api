using Invoice.AI.Vendor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.CoreAPI.Controllers;   // use your CoreAPI controllers namespace

/// <summary>
/// Thin web endpoint for the vendor question service in the Invoice.AI class library.
/// The controller belongs in the web project (Invoice.CoreAPI); the logic stays in Invoice.AI.
/// Follow the pattern of your other controllers (ApiControllerBase RunAsync/Done/Fail, or ApiResponse) if you prefer;
/// the service call is the same.
/// </summary>
[ApiController]
[Route("api/v1/ai/vendor")]
[Authorize]
public class VendorAskController(VendorAskService service) : ControllerBase
{
    // POST api/v1/ai/vendor/ask   { "questions": ["...", "..."] }   or   { "question": "..." }
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] VendorAskRequest request, CancellationToken ct)
    {
        try
        {
            // The same JWT goes on to IMS.MCP.Server, which forwards it to CoreAPI and Invoice.RAG.
            var result = await service.AskAsync(request, Request.Headers.Authorization.ToString(), ct);
            return Ok(new { success = true, message = "Answer generated.", data = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
