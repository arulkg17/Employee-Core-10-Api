using Invoice.RAG.Agent;
using Invoice.RAG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Invoice.RAG.Controllers;

[ApiController]
[Route("api/v1/agent")]
[Authorize]
public class AgentController(AgentService agent) : ControllerBase
{
    // POST api/v1/agent/ask
    //   { "questions": ["What are the payment terms of VEND004?", "How many purchase orders does VEND004 have?"] }
    //   or { "question": "One message, even with several questions in it" }
    // The model picks the tools: business data from CoreAPI (structured), agreement text (unstructured), or both.
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AgentRequest request, CancellationToken ct)
        => Ok(new ApiResult<AgentResponse>(true, "Answer generated.", await agent.AskAsync(request, ct)));
}
